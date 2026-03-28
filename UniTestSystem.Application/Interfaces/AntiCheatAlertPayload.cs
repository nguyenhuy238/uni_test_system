namespace UniTestSystem.Application.Interfaces;

public sealed class AntiCheatAlertPayload
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime Timestamp { get; set; }
    public int ViolationCount { get; set; }
    public string Severity { get; set; } = "Warning";
}
