using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class UserTestService : IUserTestService
{
    private static readonly SessionStatus[] SubmittedStatuses =
    {
        SessionStatus.Submitted,
        SessionStatus.AutoSubmitted,
        SessionStatus.Graded
    };

    private readonly IRepository<Test> _testRepo;
    private readonly IRepository<Session> _sessionRepo;
    private readonly IRepository<Result> _resultRepo;
    private readonly AssessmentService _assessmentService;
    private readonly TestService _testService;

    public UserTestService(
        IRepository<Test> testRepo,
        IRepository<Session> sessionRepo,
        IRepository<Result> resultRepo,
        AssessmentService assessmentService,
        TestService testService)
    {
        _testRepo = testRepo;
        _sessionRepo = sessionRepo;
        _resultRepo = resultRepo;
        _assessmentService = assessmentService;
        _testService = testService;
    }

    public Task<List<Test>> GetAvailablePublishedTestsAsync()
    {
        return _testRepo.GetAllAsync(x => x.IsPublished && x.Type == TestType.Test);
    }

    public Task<Test?> GetPublishedTestByIdAsync(string id)
    {
        return _testRepo.FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
    }

    public async Task<string?> StartOrResumeSessionAsync(string testId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var test = await GetPublishedTestByIdAsync(testId);
        if (test == null) return null;

        var session = await _testService.StartAsync(testId, userId);
        return session.Id;
    }

    public async Task<List<UserTestResultItem>> GetUserResultsAsync(string userId)
    {
        var tests = await _testRepo.GetAllAsync();
        var testMap = tests.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var results = await _resultRepo.GetAllAsync(x => x.UserId == userId);

        return results
            .Select(r =>
            {
                testMap.TryGetValue(r.TestId, out var test);
                return new UserTestResultItem
                {
                    Id = r.Id,
                    Title = test?.Title ?? "Untitled",
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    SubmitTime = r.SubmitTime,
                    Status = r.Status,
                    Type = test?.Type ?? TestType.Test
                };
            })
            .OrderByDescending(x => x.SubmitTime)
            .ToList();
    }

    public async Task<UserTestResultItem?> GetUserResultByIdAsync(string userId, string resultId)
    {
        var result = await _resultRepo.FirstOrDefaultAsync(x => x.Id == resultId && x.UserId == userId);
        if (result == null) return null;

        var test = await _testRepo.FirstOrDefaultAsync(x => x.Id == result.TestId);
        return new UserTestResultItem
        {
            Id = result.Id,
            Title = test?.Title ?? "Untitled",
            Score = result.Score,
            MaxScore = result.MaxScore,
            SubmitTime = result.SubmitTime,
            Status = result.Status,
            Type = test?.Type ?? TestType.Test
        };
    }

    public async Task<MyTestsOverviewData> GetMyTestsOverviewAsync(string userId, DateTime nowUtc)
    {
        var availableIds = await _assessmentService.GetAvailableTestIdsAsync(userId, nowUtc);
        var allTests = await _testRepo.GetAllAsync();
        var sessions = await _sessionRepo.GetAllAsync(x => x.UserId == userId);

        var startedTestIds = sessions.Select(x => x.TestId).ToHashSet(StringComparer.Ordinal);
        var availableTests = allTests
            .Where(x => availableIds.Contains(x.Id) && !startedTestIds.Contains(x.Id))
            .ToList();

        var submittedTestIds = sessions
            .Where(x => SubmittedStatuses.Contains(x.Status))
            .Select(x => x.TestId)
            .ToHashSet(StringComparer.Ordinal);

        var inProgress = sessions
            .Where(x => x.Status == SessionStatus.InProgress && !submittedTestIds.Contains(x.TestId))
            .GroupBy(x => x.TestId)
            .Select(g => g.OrderByDescending(x => x.StartAt).First())
            .OrderByDescending(x => x.StartAt)
            .ToList();

        var submitted = sessions
            .Where(x => SubmittedStatuses.Contains(x.Status))
            .OrderByDescending(x => x.EndAt ?? x.StartAt)
            .ToList();

        return new MyTestsOverviewData
        {
            AvailableTests = availableTests,
            InProgressSessions = inProgress,
            SubmittedSessions = submitted,
            TestTitles = allTests.ToDictionary(x => x.Id, x => x.Title, StringComparer.Ordinal)
        };
    }
}
