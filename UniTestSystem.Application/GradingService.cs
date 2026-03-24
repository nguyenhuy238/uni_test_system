using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Application
{
    public class GradingService : IGradingService
    {
        private const string ActionRegradeRequested = "RegradeRequested";
        private const string ActionRegradeApproved = "RegradeResolvedApproved";
        private const string ActionRegradeRejected = "RegradeResolvedRejected";
        private const string ActionGradeLocked = "GradeLocked";
        private const string ActionGradeUnlocked = "GradeUnlocked";

        private readonly IRepository<Session> _sRepo;
        private readonly IRepository<StudentAnswer> _saRepo;
        private readonly IRepository<Test> _tRepo;
        private readonly IRepository<SessionLog> _logRepo;
        private readonly IRepository<Notification> _notificationRepo;

        public GradingService(
            IRepository<Session> sRepo,
            IRepository<StudentAnswer> saRepo,
            IRepository<Test> tRepo,
            IRepository<SessionLog> logRepo,
            IRepository<Notification> notificationRepo)
        {
            _sRepo = sRepo;
            _saRepo = saRepo;
            _tRepo = tRepo;
            _logRepo = logRepo;
            _notificationRepo = notificationRepo;
        }

        public async Task<List<Session>> GetPendingGradingSessionsAsync(string lecturerId)
        {
            // Fetch sessions that have essays and are not yet finalized.
            // In a real scenario, we'd filter by lecturer's assigned courses.
            return await _sRepo.Query()
                .Include(s => s.Test)
                .Include(s => s.User)
                .Where(s => (s.Status == SessionStatus.Submitted || s.Status == SessionStatus.AutoSubmitted || s.Status == SessionStatus.Graded))
                .Where(s => s.StudentAnswers.Any(sa => sa.Question.Type == QType.Essay))
                .OrderByDescending(s => s.EndAt)
                .ToListAsync();
        }

        public async Task<Session> GetSessionForGradingAsync(string sessionId)
        {
            return await _sRepo.Query()
                .Include(s => s.User)
                .Include(s => s.Test)
                    .ThenInclude(t => t.TestQuestions)
                .Include(s => s.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(s => s.Id == sessionId) 
                ?? throw new Exception("Session not found");
        }

        public async Task GradeEssayAsync(string sessionId, string questionId, decimal score, string? comment)
        {
            if (await IsGradeLockedAsync(sessionId))
                throw new Exception("Grade is locked. Unlock before editing.");

            var sa = await _saRepo.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.QuestionId == questionId)
                ?? throw new Exception("Answer not found");

            if (score < 0) score = 0;
            sa.Score = score;
            sa.Comment = comment;
            sa.GradedAt = DateTime.UtcNow;

            await _saRepo.UpdateAsync(sa);
            
            // Re-calculate TotalScore
            await RecalculateTotalScoreAsync(sessionId);
        }

        private async Task RecalculateTotalScoreAsync(string sessionId)
        {
            var s = await _sRepo.Query()
                .Include(x => x.StudentAnswers)
                .ThenInclude(x => x.Question)
                .FirstOrDefaultAsync(x => x.Id == sessionId);

            if (s != null)
            {
                // Manual score is the sum of scores of questions that were manually graded (Essays)
                // Note: In this system, sa.Score is already the points (not a 0..1 scale)
                s.ManualScore = s.StudentAnswers
                    .Where(x => x.Question.Type == QType.Essay && x.GradedAt != null)
                    .Sum(x => x.Score);
                
                s.TotalScore = s.AutoScore + s.ManualScore;
                if (s.MaxScore > 0)
                {
                    s.Percent = Math.Round((s.TotalScore / s.MaxScore) * 100, 2);
                }
                
                await _sRepo.UpdateAsync(s);
            }
        }

        public async Task FinalizeGradingAsync(string sessionId)
        {
            if (await IsGradeLockedAsync(sessionId))
                throw new Exception("Grade is locked. Unlock before finalizing.");

            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sessionId) 
                ?? throw new Exception("Session not found");

            s.Status = SessionStatus.Graded;
            s.GradedAt = DateTime.UtcNow;

            await _sRepo.UpdateAsync(s);
        }

        public async Task<bool> IsGradeLockedAsync(string sessionId)
        {
            var latest = await _logRepo.Query()
                .Where(l => l.SessionId == sessionId &&
                            (l.ActionType == ActionGradeLocked || l.ActionType == ActionGradeUnlocked))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            return latest?.ActionType == ActionGradeLocked;
        }

        public async Task LockGradeAsync(string sessionId, string actor, string? note = null)
        {
            if (await IsGradeLockedAsync(sessionId)) return;
            await _logRepo.InsertAsync(new SessionLog
            {
                SessionId = sessionId,
                ActionType = ActionGradeLocked,
                Detail = BuildActorDetail(actor, note),
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task UnlockGradeAsync(string sessionId, string actor, string? note = null)
        {
            if (!await IsGradeLockedAsync(sessionId)) return;
            await _logRepo.InsertAsync(new SessionLog
            {
                SessionId = sessionId,
                ActionType = ActionGradeUnlocked,
                Detail = BuildActorDetail(actor, note),
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<bool> HasPendingRegradeRequestAsync(string sessionId)
        {
            var latest = await GetLatestRegradeLifecycleLogAsync(sessionId);
            return latest?.ActionType == ActionRegradeRequested;
        }

        public async Task<string?> GetPendingRegradeReasonAsync(string sessionId)
        {
            if (!await HasPendingRegradeRequestAsync(sessionId)) return null;

            var request = await _logRepo.Query()
                .Where(l => l.SessionId == sessionId && l.ActionType == ActionRegradeRequested)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            return request?.Detail;
        }

        public async Task<List<RegradeRequestItemVm>> GetPendingRegradeRequestsAsync(string lecturerId)
        {
            // lecturerId currently unused due to legacy assignment model; kept for future course-bound filtering.
            var requestLogs = await _logRepo.Query()
                .Where(l => l.ActionType == ActionRegradeRequested)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var resolvedLogs = await _logRepo.Query()
                .Where(l => l.ActionType == ActionRegradeApproved || l.ActionType == ActionRegradeRejected)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var pendingRequestBySession = requestLogs
                .GroupBy(l => l.SessionId)
                .Select(g => g.OrderByDescending(x => x.Timestamp).First())
                .Where(latestRequest =>
                {
                    var latestResolvedAt = resolvedLogs
                        .Where(r => r.SessionId == latestRequest.SessionId)
                        .Select(r => r.Timestamp)
                        .DefaultIfEmpty(DateTime.MinValue)
                        .Max();
                    return latestRequest.Timestamp > latestResolvedAt;
                })
                .ToList();

            if (!pendingRequestBySession.Any())
                return new List<RegradeRequestItemVm>();

            var sessionIds = pendingRequestBySession.Select(x => x.SessionId).Distinct().ToList();
            var sessions = await _sRepo.Query()
                .Include(s => s.User)
                .Include(s => s.Test)
                .Where(s => sessionIds.Contains(s.Id))
                .ToListAsync();

            var lockLogs = await _logRepo.Query()
                .Where(l => sessionIds.Contains(l.SessionId) &&
                            (l.ActionType == ActionGradeLocked || l.ActionType == ActionGradeUnlocked))
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            var latestLockBySession = lockLogs
                .GroupBy(l => l.SessionId)
                .ToDictionary(g => g.Key, g => g.First().ActionType == ActionGradeLocked);

            var sessionMap = sessions.ToDictionary(s => s.Id, s => s);

            var result = new List<RegradeRequestItemVm>();
            foreach (var req in pendingRequestBySession)
            {
                if (!sessionMap.TryGetValue(req.SessionId, out var s)) continue;

                result.Add(new RegradeRequestItemVm
                {
                    SessionId = s.Id,
                    StudentId = s.UserId,
                    StudentName = s.User?.Name ?? s.UserId,
                    TestId = s.TestId,
                    TestTitle = s.Test?.Title ?? s.TestId,
                    RequestedAt = req.Timestamp,
                    Reason = req.Detail ?? "",
                    IsGradeLocked = latestLockBySession.TryGetValue(s.Id, out var locked) && locked
                });
            }

            return result.OrderByDescending(x => x.RequestedAt).ToList();
        }

        public async Task RequestRegradeAsync(string sessionId, string studentId, string reason, string? ipAddress)
        {
            var session = await _sRepo.Query()
                .Include(s => s.Test)
                .FirstOrDefaultAsync(s => s.Id == sessionId)
                ?? throw new Exception("Session not found");

            if (!string.Equals(session.UserId, studentId, StringComparison.Ordinal))
                throw new Exception("You cannot request regrade for this session.");

            if (session.Status == SessionStatus.InProgress)
                throw new Exception("You must submit before requesting regrade.");

            if (await HasPendingRegradeRequestAsync(sessionId))
                throw new Exception("A regrade request is already pending for this session.");

            var cleanReason = (reason ?? "").Trim();
            if (string.IsNullOrWhiteSpace(cleanReason))
                throw new Exception("Regrade reason is required.");

            await _logRepo.InsertAsync(new SessionLog
            {
                SessionId = sessionId,
                ActionType = ActionRegradeRequested,
                Detail = cleanReason,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });

            await _notificationRepo.InsertAsync(new Notification
            {
                UserId = studentId,
                Title = "Regrade Request Submitted",
                Message = $"Your regrade request for session {sessionId} has been submitted.",
                Link = $"/mytests/result/{sessionId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task ResolveRegradeRequestAsync(string sessionId, string actor, bool approved, string? resolutionNote)
        {
            if (!await HasPendingRegradeRequestAsync(sessionId))
                throw new Exception("No pending regrade request found.");

            var session = await _sRepo.FirstOrDefaultAsync(x => x.Id == sessionId)
                ?? throw new Exception("Session not found");

            var action = approved ? ActionRegradeApproved : ActionRegradeRejected;
            var detail = BuildActorDetail(actor, resolutionNote);

            await _logRepo.InsertAsync(new SessionLog
            {
                SessionId = sessionId,
                ActionType = action,
                Detail = detail,
                Timestamp = DateTime.UtcNow
            });

            if (approved)
            {
                // Move back to submitted state so lecturer can update grades.
                session.Status = SessionStatus.Submitted;
                session.UpdatedAt = DateTime.UtcNow;
                await _sRepo.UpdateAsync(session);
            }

            await _notificationRepo.InsertAsync(new Notification
            {
                UserId = session.UserId,
                Title = approved ? "Regrade Approved" : "Regrade Rejected",
                Message = approved
                    ? $"Your regrade request for session {sessionId} was approved. Lecturer will review your answers."
                    : $"Your regrade request for session {sessionId} was rejected.",
                Link = $"/mytests/result/{sessionId}",
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<List<SessionLog>> GetModerationLogsAsync(string sessionId)
        {
            return await _logRepo.Query()
                .Where(l => l.SessionId == sessionId &&
                            (l.ActionType == ActionRegradeRequested ||
                             l.ActionType == ActionRegradeApproved ||
                             l.ActionType == ActionRegradeRejected ||
                             l.ActionType == ActionGradeLocked ||
                             l.ActionType == ActionGradeUnlocked))
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        private async Task<SessionLog?> GetLatestRegradeLifecycleLogAsync(string sessionId)
        {
            return await _logRepo.Query()
                .Where(l => l.SessionId == sessionId &&
                            (l.ActionType == ActionRegradeRequested ||
                             l.ActionType == ActionRegradeApproved ||
                             l.ActionType == ActionRegradeRejected))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();
        }

        private static string BuildActorDetail(string actor, string? note)
        {
            var cleanNote = string.IsNullOrWhiteSpace(note) ? "-" : note.Trim();
            return $"Actor={actor}; Note={cleanNote}";
        }
    }
}
