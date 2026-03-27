using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;

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
            // lecturerId currently unused due to legacy assignment model; kept for future course-bound filtering.
            var spec = new Specification<Session>(s =>
                    s.Status == SessionStatus.Submitted ||
                    s.Status == SessionStatus.AutoSubmitted ||
                    s.Status == SessionStatus.Graded)
                .Include(s => s.Test!)
                .Include(s => s.User!)
                .Include("StudentAnswers.Question");

            var sessions = await _sRepo.ListAsync(spec);
            return sessions
                .Where(s => s.StudentAnswers.Any(sa => sa.Question != null))
                .OrderByDescending(s => s.EndAt)
                .ToList();
        }

        public async Task<Session> GetSessionForGradingAsync(string sessionId)
        {
            var spec = new Specification<Session>(s => s.Id == sessionId)
                .Include(s => s.User!)
                .Include(s => s.Test!)
                .Include("Test.TestQuestions")
                .Include("Test.QuestionSnapshots")
                .Include("StudentAnswers.Question.Options");

            return await _sRepo.FirstOrDefaultAsync(spec)
                ?? throw new Exception("Session not found");
        }

        public async Task GradeAnswerAsync(string sessionId, string questionId, decimal score, string? comment)
        {
            if (await IsGradeLockedAsync(sessionId))
                throw new Exception("Grade is locked. Unlock before editing.");

            var maxPoints = await ResolveMaxPointsAsync(sessionId, questionId);
            var answerSpec = new Specification<StudentAnswer>(x => x.SessionId == sessionId && x.QuestionId == questionId)
                .Include(x => x.Question!);
            var sa = await _saRepo.FirstOrDefaultAsync(answerSpec)
                ?? throw new Exception("Answer not found");

            if (score < 0) score = 0;
            if (maxPoints > 0m && score > maxPoints) score = maxPoints;
            var roundedScore = Math.Round(score, 2, MidpointRounding.AwayFromZero);
            var cleanedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
            var existingComment = string.IsNullOrWhiteSpace(sa.Comment) ? null : sa.Comment.Trim();
            var shouldMarkAsManuallyGraded =
                sa.GradedAt.HasValue ||
                roundedScore != sa.Score ||
                !string.Equals(existingComment, cleanedComment, StringComparison.Ordinal);

            sa.Score = roundedScore;
            sa.Comment = cleanedComment;
            if (shouldMarkAsManuallyGraded)
            {
                sa.GradedAt = DateTime.UtcNow;
            }

            await _saRepo.UpdateAsync(sa);
            await RecalculateTotalScoreAsync(sessionId);
        }

        public async Task GradeEssayAsync(string sessionId, string questionId, decimal score, string? comment)
        {
            await GradeAnswerAsync(sessionId, questionId, score, comment);
        }

        private async Task RecalculateTotalScoreAsync(string sessionId)
        {
            var spec = new Specification<Session>(x => x.Id == sessionId)
                .Include("StudentAnswers.Question")
                .Include("Test.TestQuestions")
                .Include("Test.QuestionSnapshots");
            var s = await _sRepo.FirstOrDefaultAsync(spec);

            if (s != null)
            {
                var pointsByQuestion = BuildPointsMap(s.Test);
                decimal autoScore = 0m;
                decimal manualScore = 0m;

                foreach (var answer in s.StudentAnswers)
                {
                    var maxPoints = pointsByQuestion.TryGetValue(answer.QuestionId, out var p) ? p : 1m;
                    if (maxPoints <= 0) maxPoints = 1m;

                    var clampedScore = Math.Clamp(answer.Score, 0m, maxPoints);
                    var isManualBucket = answer.Question?.Type == QType.Essay || answer.GradedAt.HasValue;

                    if (isManualBucket)
                    {
                        manualScore += clampedScore;
                    }
                    else
                    {
                        autoScore += clampedScore;
                    }
                }

                s.AutoScore = Math.Round(autoScore, 2, MidpointRounding.AwayFromZero);
                s.ManualScore = Math.Round(manualScore, 2, MidpointRounding.AwayFromZero);
                s.TotalScore = Math.Round(s.AutoScore + s.ManualScore, 2, MidpointRounding.AwayFromZero);
                if (s.MaxScore > 0)
                {
                    s.Percent = Math.Round((s.TotalScore / s.MaxScore) * 100, 2);
                }

                if (s.Test != null)
                {
                    s.IsPassed = s.TotalScore >= s.Test.PassScore;
                }

                await _sRepo.UpdateAsync(s);
            }
        }

        public async Task FinalizeGradingAsync(string sessionId)
        {
            if (await IsGradeLockedAsync(sessionId))
                throw new Exception("Grade is locked. Unlock before finalizing.");

            await RecalculateTotalScoreAsync(sessionId);

            var s = await _sRepo.FirstOrDefaultAsync(x => x.Id == sessionId) 
                ?? throw new Exception("Session not found");

            s.Status = SessionStatus.Graded;
            s.GradedAt = DateTime.UtcNow;

            await _sRepo.UpdateAsync(s);
        }

        public async Task<bool> IsGradeLockedAsync(string sessionId)
        {
            var latest = (await _logRepo.GetAllAsync(l =>
                    l.SessionId == sessionId &&
                    (l.ActionType == ActionGradeLocked || l.ActionType == ActionGradeUnlocked)))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

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

            var request = (await _logRepo.GetAllAsync(l =>
                    l.SessionId == sessionId &&
                    l.ActionType == ActionRegradeRequested))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            return request?.Detail;
        }

        public async Task<List<RegradeRequestItemVm>> GetPendingRegradeRequestsAsync(string lecturerId)
        {
            // lecturerId currently unused due to legacy assignment model; kept for future course-bound filtering.
            var requestLogs = (await _logRepo.GetAllAsync(l => l.ActionType == ActionRegradeRequested))
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            var resolvedLogs = (await _logRepo.GetAllAsync(l =>
                    l.ActionType == ActionRegradeApproved ||
                    l.ActionType == ActionRegradeRejected))
                .OrderByDescending(l => l.Timestamp)
                .ToList();

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
            var sessionSpec = new Specification<Session>(s => sessionIds.Contains(s.Id))
                .Include(s => s.User!)
                .Include(s => s.Test!);
            var sessions = await _sRepo.ListAsync(sessionSpec);

            var lockLogs = (await _logRepo.GetAllAsync(l =>
                    sessionIds.Contains(l.SessionId) &&
                    (l.ActionType == ActionGradeLocked || l.ActionType == ActionGradeUnlocked)))
                .OrderByDescending(l => l.Timestamp)
                .ToList();

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
            var spec = new Specification<Session>(s => s.Id == sessionId)
                .Include(s => s.Test!);
            var session = await _sRepo.FirstOrDefaultAsync(spec)
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
            return (await _logRepo.GetAllAsync(l =>
                    l.SessionId == sessionId &&
                    (l.ActionType == ActionRegradeRequested ||
                     l.ActionType == ActionRegradeApproved ||
                     l.ActionType == ActionRegradeRejected ||
                     l.ActionType == ActionGradeLocked ||
                     l.ActionType == ActionGradeUnlocked)))
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        }

        private async Task<SessionLog?> GetLatestRegradeLifecycleLogAsync(string sessionId)
        {
            return (await _logRepo.GetAllAsync(l =>
                    l.SessionId == sessionId &&
                    (l.ActionType == ActionRegradeRequested ||
                     l.ActionType == ActionRegradeApproved ||
                     l.ActionType == ActionRegradeRejected)))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();
        }

        private static string BuildActorDetail(string actor, string? note)
        {
            var cleanNote = string.IsNullOrWhiteSpace(note) ? "-" : note.Trim();
            return $"Actor={actor}; Note={cleanNote}";
        }

        private static Dictionary<string, decimal> BuildPointsMap(Test? test)
        {
            var map = new Dictionary<string, decimal>(StringComparer.Ordinal);
            if (test == null)
            {
                return map;
            }

            if (test.TestQuestions != null && test.TestQuestions.Count > 0)
            {
                foreach (var item in test.TestQuestions)
                {
                    if (string.IsNullOrWhiteSpace(item.QuestionId)) continue;
                    map[item.QuestionId] = item.Points > 0m ? item.Points : 1m;
                }
            }

            if (map.Count == 0 && test.QuestionSnapshots != null && test.QuestionSnapshots.Count > 0)
            {
                foreach (var item in test.QuestionSnapshots)
                {
                    if (string.IsNullOrWhiteSpace(item.OriginalQuestionId)) continue;
                    map[item.OriginalQuestionId] = item.Points > 0m ? item.Points : 1m;
                }
            }

            return map;
        }

        private async Task<decimal> ResolveMaxPointsAsync(string sessionId, string questionId)
        {
            var sessionSpec = new Specification<Session>(x => x.Id == sessionId)
                .Include("Test.TestQuestions")
                .Include("Test.QuestionSnapshots");
            var session = await _sRepo.FirstOrDefaultAsync(sessionSpec);
            if (session?.Test == null)
            {
                return 1m;
            }

            var pointsByQuestion = BuildPointsMap(session.Test);
            if (pointsByQuestion.TryGetValue(questionId, out var points) && points > 0m)
            {
                return points;
            }

            return 1m;
        }
    }
}
