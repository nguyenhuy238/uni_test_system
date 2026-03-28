namespace UniTestSystem.Application.Interfaces;

public interface IExamHubNotifier
{
    Task NotifySessionStartedAsync(AdminSessionItem item);
    Task NotifySessionSubmittedAsync(string testId, SubmitSessionData data, string userName);
    Task NotifyAntiCheatAlertAsync(string testId, AntiCheatAlertPayload alert);
    Task PushTimerSyncAsync(string sessionId, int remainingSeconds, bool running);
    Task NotifyAutoSubmittedAsync(string sessionId);
}
