using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IResultsService
{
    Task<List<AdminResultItem>> GetResultsAsync(string? testId = null);
    Task<ResultStatsData> GetStatsAsync();
    Task<List<ResultExportItem>> GetExportDataAsync(string? testId = null);
    Task<int> GetResultCountAsync();

    Task<FeedbackCreateContext> GetFeedbackCreateContextAsync(string sessionId, string userId);
    Task<FeedbackEditContext> GetFeedbackEditContextAsync(string feedbackId, string userId);
    Task<FeedbackCommandResult> CreateFeedbackAsync(string sessionId, string userId, string content, int rating);
    Task<FeedbackCommandResult> UpdateFeedbackAsync(string feedbackId, string userId, string content, int rating);

    Task<AdminFeedbackListData> GetAdminFeedbacksAsync(string? testId = null);
    Task<AdminFeedbackListData> GetLecturerFeedbacksAsync(string lecturerId, string? testId = null);
}

public sealed class AdminResultItem
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public TestType TestType { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime SubmitTime { get; set; }
    public SessionStatus Status { get; set; }
}

public sealed class ResultExportItem
{
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime SubmitTime { get; set; }
    public SessionStatus Status { get; set; }
}

public sealed class ResultStatsData
{
    public int TotalSubmissions { get; set; }
    public decimal AverageScore { get; set; }
    public decimal PassRate { get; set; }
    public Dictionary<string, int> TestsByType { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> SubmissionsByMonth { get; set; } = new(StringComparer.Ordinal);
}

public enum FeedbackAccessStatus
{
    Success,
    NotFound,
    Forbidden,
    InProgress
}

public enum FeedbackCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    InProgress,
    Conflict
}

public sealed class FeedbackCreateContext
{
    public FeedbackAccessStatus Status { get; set; }
    public string SessionId { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public string? ExistingFeedbackId { get; set; }
}

public sealed class FeedbackEditContext
{
    public FeedbackAccessStatus Status { get; set; }
    public string FeedbackId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public string Content { get; set; } = "";
    public int Rating { get; set; }
}

public sealed class FeedbackCommandResult
{
    public FeedbackCommandStatus Status { get; set; }
    public string SessionId { get; set; } = "";
    public string? FeedbackId { get; set; }
    public string? ExistingFeedbackId { get; set; }
}

public sealed class AdminFeedbackListData
{
    public List<AdminFeedbackItem> Items { get; set; } = new();
    public List<TestLookupItem> Tests { get; set; } = new();
}

public sealed class AdminFeedbackItem
{
    public string FeedbackId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string TestTitle { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = "";
}

public sealed class TestLookupItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
}
