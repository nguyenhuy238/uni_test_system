using Microsoft.AspNetCore.SignalR;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Hubs;

namespace UniTestSystem.Services;

public sealed class ExamHubNotifier : IExamHubNotifier
{
    private readonly IHubContext<ExamSessionHub> _examHubContext;
    private readonly IHubContext<ProctorHub> _proctorHubContext;

    public ExamHubNotifier(
        IHubContext<ExamSessionHub> examHubContext,
        IHubContext<ProctorHub> proctorHubContext)
    {
        _examHubContext = examHubContext;
        _proctorHubContext = proctorHubContext;
    }

    public Task PushTimerSyncAsync(string sessionId, int remainingSeconds, bool running)
    {
        return _examHubContext.Clients.Group(SessionGroup(sessionId)).SendAsync("TimerSync", new
        {
            sessionId,
            remainingSeconds,
            running
        });
    }

    public Task NotifyAutoSubmittedAsync(string sessionId)
    {
        return _examHubContext.Clients.Group(SessionGroup(sessionId)).SendAsync("SessionAutoSubmitted", new
        {
            sessionId,
            timestamp = DateTime.UtcNow
        });
    }

    public Task NotifySessionStartedAsync(AdminSessionItem item)
    {
        return NotifyProctorsAsync(item.TestId, "SessionStarted", item);
    }

    public Task NotifySessionSubmittedAsync(string testId, SubmitSessionData data, string userName)
    {
        return NotifyProctorsAsync(testId, "SessionSubmitted", new
        {
            testId,
            userName,
            data
        });
    }

    public Task NotifyAntiCheatAlertAsync(string testId, AntiCheatAlertPayload alert)
    {
        return NotifyProctorsAsync(testId, "AntiCheatAlert", alert);
    }

    private async Task NotifyProctorsAsync(string testId, string eventName, object payload)
    {
        await _proctorHubContext.Clients.Group(ProctorTestGroup(testId)).SendAsync(eventName, payload);
        await _proctorHubContext.Clients.Group(GlobalProctorGroup).SendAsync(eventName, payload);
    }

    private static string SessionGroup(string sessionId) => $"session_{sessionId}";
    private static string ProctorTestGroup(string testId) => $"proctor_{testId}";
    private const string GlobalProctorGroup = "proctor_global";
}
