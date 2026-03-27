using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class ResultsService : IResultsService
{
    private readonly IRepository<Result> _resultRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Test> _testRepo;
    private readonly IRepository<Feedback> _feedbackRepo;
    private readonly IRepository<Session> _sessionRepo;

    public ResultsService(
        IRepository<Result> resultRepo,
        IRepository<User> userRepo,
        IRepository<Test> testRepo,
        IRepository<Feedback> feedbackRepo,
        IRepository<Session> sessionRepo)
    {
        _resultRepo = resultRepo;
        _userRepo = userRepo;
        _testRepo = testRepo;
        _feedbackRepo = feedbackRepo;
        _sessionRepo = sessionRepo;
    }

    public async Task<List<AdminResultItem>> GetResultsAsync(string? testId = null)
    {
        var results = await _resultRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        var tests = await _testRepo.GetAllAsync();

        var query = results.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(testId))
        {
            query = query.Where(x => x.TestId == testId);
        }

        return query
            .Join(users, r => r.UserId, u => u.Id, (r, u) => new { r, u })
            .Join(tests, ru => ru.r.TestId, t => t.Id, (ru, t) => new AdminResultItem
            {
                Id = ru.r.Id,
                UserName = ru.u.Name,
                UserEmail = ru.u.Email,
                TestTitle = t.Title,
                TestType = t.Type,
                Score = ru.r.Score,
                MaxScore = ru.r.MaxScore,
                SubmitTime = ru.r.SubmitTime,
                Status = ru.r.Status
            })
            .OrderByDescending(x => x.SubmitTime)
            .ToList();
    }

    public async Task<ResultStatsData> GetStatsAsync()
    {
        var results = await _resultRepo.GetAllAsync();
        var tests = await _testRepo.GetAllAsync();

        return new ResultStatsData
        {
            TotalSubmissions = results.Count,
            AverageScore = results.Any() ? results.Average(x => x.Score) : 0m,
            PassRate = results.Any() ? (decimal)results.Count(x => x.Score >= 5) / results.Count * 100m : 0m,
            TestsByType = tests
                .GroupBy(x => x.Type)
                .ToDictionary(x => x.Key.ToString(), x => x.Count(), StringComparer.Ordinal),
            SubmissionsByMonth = results
                .GroupBy(x => new { x.SubmitTime.Year, x.SubmitTime.Month })
                .ToDictionary(x => $"{x.Key.Year}-{x.Key.Month:D2}", x => x.Count(), StringComparer.Ordinal)
        };
    }

    public async Task<List<ResultExportItem>> GetExportDataAsync(string? testId = null)
    {
        var results = await _resultRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        var tests = await _testRepo.GetAllAsync();

        var query = results.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(testId))
        {
            query = query.Where(x => x.TestId == testId);
        }

        return query
            .Join(users, r => r.UserId, u => u.Id, (r, u) => new { r, u })
            .Join(tests, ru => ru.r.TestId, t => t.Id, (ru, t) => new ResultExportItem
            {
                UserName = ru.u.Name,
                UserEmail = ru.u.Email,
                TestTitle = t.Title,
                Score = ru.r.Score,
                MaxScore = ru.r.MaxScore,
                SubmitTime = ru.r.SubmitTime,
                Status = ru.r.Status
            })
            .ToList();
    }

    public async Task<int> GetResultCountAsync()
    {
        return (await _resultRepo.GetAllAsync()).Count;
    }

    public async Task<FeedbackCreateContext> GetFeedbackCreateContextAsync(string sessionId, string userId)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session == null)
        {
            return new FeedbackCreateContext
            {
                Status = FeedbackAccessStatus.NotFound
            };
        }

        if (!string.Equals(session.UserId, userId, StringComparison.Ordinal))
        {
            return new FeedbackCreateContext
            {
                Status = FeedbackAccessStatus.Forbidden
            };
        }

        if (session.Status == SessionStatus.InProgress)
        {
            return new FeedbackCreateContext
            {
                Status = FeedbackAccessStatus.InProgress
            };
        }

        var existing = await _feedbackRepo.FirstOrDefaultAsync(x => x.SessionId == sessionId && !x.IsDeleted);
        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);

        return new FeedbackCreateContext
        {
            Status = FeedbackAccessStatus.Success,
            SessionId = sessionId,
            TestTitle = test?.Title ?? session.TestId,
            ExistingFeedbackId = existing?.Id
        };
    }

    public async Task<FeedbackEditContext> GetFeedbackEditContextAsync(string feedbackId, string userId)
    {
        var feedback = await _feedbackRepo.FirstOrDefaultAsync(x => x.Id == feedbackId && !x.IsDeleted);
        if (feedback == null)
        {
            return new FeedbackEditContext
            {
                Status = FeedbackAccessStatus.NotFound
            };
        }

        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == feedback.SessionId);
        if (session == null)
        {
            return new FeedbackEditContext
            {
                Status = FeedbackAccessStatus.NotFound
            };
        }

        if (!string.Equals(session.UserId, userId, StringComparison.Ordinal))
        {
            return new FeedbackEditContext
            {
                Status = FeedbackAccessStatus.Forbidden
            };
        }

        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == session.TestId);
        return new FeedbackEditContext
        {
            Status = FeedbackAccessStatus.Success,
            FeedbackId = feedback.Id,
            SessionId = feedback.SessionId,
            Content = feedback.Content,
            Rating = feedback.Rating,
            TestTitle = test?.Title ?? session.TestId
        };
    }

    public async Task<FeedbackCommandResult> CreateFeedbackAsync(string sessionId, string userId, string content, int rating)
    {
        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == sessionId);
        if (session == null)
        {
            return new FeedbackCommandResult { Status = FeedbackCommandStatus.NotFound };
        }

        if (!string.Equals(session.UserId, userId, StringComparison.Ordinal))
        {
            return new FeedbackCommandResult { Status = FeedbackCommandStatus.Forbidden };
        }

        if (session.Status == SessionStatus.InProgress)
        {
            return new FeedbackCommandResult { Status = FeedbackCommandStatus.InProgress };
        }

        var existing = await _feedbackRepo.FirstOrDefaultAsync(x => x.SessionId == sessionId && !x.IsDeleted);
        if (existing != null)
        {
            return new FeedbackCommandResult
            {
                Status = FeedbackCommandStatus.Conflict,
                ExistingFeedbackId = existing.Id
            };
        }

        var feedback = new Feedback
        {
            SessionId = sessionId,
            Content = (content ?? string.Empty).Trim(),
            Rating = rating,
            CreatedAt = DateTime.UtcNow
        };

        await _feedbackRepo.InsertAsync(feedback);
        return new FeedbackCommandResult
        {
            Status = FeedbackCommandStatus.Success,
            SessionId = sessionId,
            FeedbackId = feedback.Id
        };
    }

    public async Task<FeedbackCommandResult> UpdateFeedbackAsync(string feedbackId, string userId, string content, int rating)
    {
        var feedback = await _feedbackRepo.FirstOrDefaultAsync(x => x.Id == feedbackId && !x.IsDeleted);
        if (feedback == null)
        {
            return new FeedbackCommandResult { Status = FeedbackCommandStatus.NotFound };
        }

        var session = await _sessionRepo.FirstOrDefaultAsync(x => x.Id == feedback.SessionId);
        if (session == null)
        {
            return new FeedbackCommandResult { Status = FeedbackCommandStatus.NotFound };
        }

        if (!string.Equals(session.UserId, userId, StringComparison.Ordinal))
        {
            return new FeedbackCommandResult { Status = FeedbackCommandStatus.Forbidden };
        }

        feedback.Content = (content ?? string.Empty).Trim();
        feedback.Rating = rating;
        feedback.UpdatedAt = DateTime.UtcNow;

        await _feedbackRepo.UpsertAsync(x => x.Id == feedback.Id, feedback);
        return new FeedbackCommandResult
        {
            Status = FeedbackCommandStatus.Success,
            SessionId = feedback.SessionId,
            FeedbackId = feedback.Id
        };
    }

    public async Task<AdminFeedbackListData> GetAdminFeedbacksAsync(string? testId = null)
    {
        var feedbacks = await _feedbackRepo.GetAllAsync(x => !x.IsDeleted);
        var sessions = await _sessionRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        var tests = await _testRepo.GetAllAsync();

        var joined = feedbacks
            .Join(sessions, f => f.SessionId, s => s.Id, (f, s) => new { f, s })
            .Join(users, fs => fs.s.UserId, u => u.Id, (fs, u) => new { fs.f, fs.s, u })
            .Join(tests, fsu => fsu.s.TestId, t => t.Id, (fsu, t) => new { fsu.f, fsu.s, fsu.u, t });

        if (!string.IsNullOrWhiteSpace(testId))
        {
            joined = joined.Where(x => x.t.Id == testId);
        }

        return new AdminFeedbackListData
        {
            Items = joined
                .Select(x => new AdminFeedbackItem
                {
                    FeedbackId = x.f.Id,
                    SessionId = x.s.Id,
                    UserName = x.u.Name,
                    UserEmail = x.u.Email,
                    TestTitle = x.t.Title,
                    CreatedAt = x.f.CreatedAt,
                    Rating = x.f.Rating,
                    Content = x.f.Content
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList(),
            Tests = tests
                .Select(x => new TestLookupItem { Id = x.Id, Title = x.Title })
                .ToList()
        };
    }
}
