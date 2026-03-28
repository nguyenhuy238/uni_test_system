using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Domain;

namespace UniTestSystem.Hubs;

[Authorize(Policy = "RequireLecturerOrStaffOrAdmin")]
public sealed class ProctorHub : Hub
{
    private readonly IRepository<Session> _sessionRepo;

    public ProctorHub(IRepository<Session> sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public async Task JoinProctorRoom(string testId)
    {
        if (string.IsNullOrWhiteSpace(testId))
        {
            throw new HubException("TestId is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ProctorTestGroup(testId));

        var activeCount = await _sessionRepo.CountAsync(s =>
            s.Status == SessionStatus.InProgress &&
            s.TestId == testId &&
            !s.IsDeleted);

        await Clients.Caller.SendAsync("ActiveSessionCount", new
        {
            testId,
            count = activeCount
        });
    }

    public async Task JoinGlobalProctor()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GlobalProctorGroup);

        var activeSessions = await _sessionRepo.GetAllAsync(s =>
            s.Status == SessionStatus.InProgress &&
            !s.IsDeleted);

        var byTest = activeSessions
            .GroupBy(s => s.TestId)
            .Select(group => new
            {
                testId = group.Key,
                count = group.Count()
            })
            .OrderByDescending(x => x.count)
            .ToList();

        await Clients.Caller.SendAsync("ActiveSessionCount", new
        {
            totalCount = activeSessions.Count,
            byTest
        });
    }

    public Task LeaveProctorRoom(string testId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ProctorTestGroup(testId));
    }

    public Task LeaveGlobalProctor()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GlobalProctorGroup);
    }

    private static string ProctorTestGroup(string testId) => $"proctor_{testId}";
    private const string GlobalProctorGroup = "proctor_global";
}
