using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public enum SessionServiceStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    BadRequest
}

public sealed class SessionRequestContext
{
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

public sealed class SessionServiceResult<T>
{
    public SessionServiceStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public sealed class StartSessionCommand
{
    public string TestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ScheduleId { get; set; }
    public string? AccessToken { get; set; }
    public bool BlockRestartAfterSubmit { get; set; } = true;
    public bool IncludeQuestionPayload { get; set; } = false;
    public bool ReturnNotFoundForUnavailableTest { get; set; } = true;
    public SessionRequestContext RequestContext { get; set; } = new();
}

public sealed class StartSessionData
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsLatestSubmitted { get; set; }
    public int DurationMinutes { get; set; }
    public int RemainingSeconds { get; set; }
    public List<SessionQuestionDto> Questions { get; set; } = new();
}

public sealed class SessionQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public QType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<Option> Options { get; set; } = new();
}

public sealed class ResumeSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public SessionRequestContext RequestContext { get; set; } = new();
}

public sealed class ResumeSessionData
{
    public Session Session { get; set; } = new();
    public string TestTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int RemainingSeconds { get; set; }
}

public sealed class SaveAnswerCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, string?> Answers { get; set; } = new();
    public SessionRequestContext RequestContext { get; set; } = new();
}

public sealed class SaveAnswerData
{
    public int UpdatedCount { get; set; }
    public DateTime At { get; set; }
}

public sealed class SubmitSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, string?> Answers { get; set; } = new();
    public SessionRequestContext RequestContext { get; set; } = new();
}

public sealed class SubmitSessionData
{
    public string Id { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percent { get; set; }
    public bool IsPassed { get; set; }
    public SessionStatus Status { get; set; }
}

public sealed class GetSessionResultCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public sealed class GetSessionResultData
{
    public Session Session { get; set; } = new();
    public string TestTitle { get; set; } = "Result";
    public bool HasPendingRegrade { get; set; }
}

public sealed class SessionTimerCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool RequireInProgressState { get; set; }
    public SessionRequestContext RequestContext { get; set; } = new();
}

public sealed class SessionTimerData
{
    public int RemainingSeconds { get; set; }
    public bool Running { get; set; }
}

public sealed class SessionTouchCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public SessionRequestContext RequestContext { get; set; } = new();
}

public sealed class SessionTouchData
{
    public DateTime At { get; set; }
    public int RemainingSeconds { get; set; }
    public bool Running { get; set; }
}

public sealed class SessionLogEventCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public SessionRequestContext RequestContext { get; set; } = new();
}

public interface ISessionService
{
    Task<SessionServiceResult<StartSessionData>> StartSessionAsync(StartSessionCommand command);
    Task<SessionServiceResult<ResumeSessionData>> ResumeSessionAsync(ResumeSessionCommand command);
    Task<SessionServiceResult<SaveAnswerData>> SaveAnswerAsync(SaveAnswerCommand command);
    Task<SessionServiceResult<SubmitSessionData>> SubmitSessionAsync(SubmitSessionCommand command);
    Task<SessionServiceResult<GetSessionResultData>> GetSessionResultAsync(GetSessionResultCommand command);
    Task<SessionServiceResult<SessionTimerData>> PauseSessionAsync(SessionTimerCommand command);
    Task<SessionServiceResult<SessionTimerData>> ResumeTimerAsync(SessionTimerCommand command);
    Task<SessionServiceResult<SessionTouchData>> TouchSessionAsync(SessionTouchCommand command);
    Task<SessionServiceResult<bool>> LogEventAsync(SessionLogEventCommand command);
}
