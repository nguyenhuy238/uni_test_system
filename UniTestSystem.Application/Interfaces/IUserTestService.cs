using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IUserTestService
{
    Task<List<Test>> GetAvailablePublishedTestsAsync();
    Task<Test?> GetPublishedTestByIdAsync(string id);
    Task<string?> StartOrResumeSessionAsync(string testId, string userId);
    Task<List<UserTestResultItem>> GetUserResultsAsync(string userId);
    Task<UserTestResultItem?> GetUserResultByIdAsync(string userId, string resultId);
    Task<MyTestsOverviewData> GetMyTestsOverviewAsync(string userId, DateTime nowUtc);
}

public sealed class UserTestResultItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime SubmitTime { get; set; }
    public SessionStatus Status { get; set; }
    public TestType Type { get; set; }
}

public sealed class MyTestsOverviewData
{
    public List<Test> AvailableTests { get; set; } = new();
    public List<Session> InProgressSessions { get; set; } = new();
    public List<Session> SubmittedSessions { get; set; } = new();
    public Dictionary<string, string> TestTitles { get; set; } = new(StringComparer.Ordinal);
}
