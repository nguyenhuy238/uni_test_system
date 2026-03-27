using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Hubs;

[Authorize]
public sealed class ExamSessionHub : Hub
{
    private readonly ISessionService _sessionService;

    public ExamSessionHub(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task JoinSession(string sessionId)
    {
        var userId = GetCurrentUserIdOrThrow();
        var touchResult = await _sessionService.TouchSessionAsync(new SessionTouchCommand
        {
            SessionId = sessionId,
            UserId = userId,
            RequestContext = BuildRequestContext()
        });

        if (touchResult.Data == null || touchResult.Status != SessionServiceStatus.Success)
        {
            throw new HubException("Unable to join this session.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        await Clients.Caller.SendAsync("TimerSync", new
        {
            sessionId,
            remainingSeconds = touchResult.Data.RemainingSeconds,
            running = touchResult.Data.Running
        });
    }

    public Task LeaveSession(string sessionId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
    }

    public async Task PauseTimer(string sessionId)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _sessionService.PauseSessionAsync(new SessionTimerCommand
        {
            SessionId = sessionId,
            UserId = userId,
            RequestContext = BuildRequestContext()
        });

        if (result.Data == null || result.Status != SessionServiceStatus.Success)
        {
            throw new HubException("Unable to pause timer.");
        }

        await Clients.Group(SessionGroup(sessionId)).SendAsync("TimerSync", new
        {
            sessionId,
            remainingSeconds = result.Data.RemainingSeconds,
            running = result.Data.Running
        });
    }

    public async Task ResumeTimer(string sessionId)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _sessionService.ResumeTimerAsync(new SessionTimerCommand
        {
            SessionId = sessionId,
            UserId = userId,
            RequireInProgressState = true,
            RequestContext = BuildRequestContext()
        });

        if (result.Data == null || result.Status != SessionServiceStatus.Success)
        {
            throw new HubException("Unable to resume timer.");
        }

        await Clients.Group(SessionGroup(sessionId)).SendAsync("TimerSync", new
        {
            sessionId,
            remainingSeconds = result.Data.RemainingSeconds,
            running = result.Data.Running
        });
    }

    private SessionRequestContext BuildRequestContext()
    {
        var httpContext = Context.GetHttpContext();
        return new SessionRequestContext
        {
            UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString()
        };
    }

    private string GetCurrentUserIdOrThrow()
    {
        var userId = Context.UserIdentifier
                     ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("User is not authenticated.");
        }

        return userId;
    }

    private static string SessionGroup(string sessionId) => $"session_{sessionId}";
}
