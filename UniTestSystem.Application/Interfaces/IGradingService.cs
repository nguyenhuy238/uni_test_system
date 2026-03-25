using UniTestSystem.Domain;
using UniTestSystem.Application.Models;

namespace UniTestSystem.Application.Interfaces;

public interface IGradingService
{
    Task<List<Session>> GetPendingGradingSessionsAsync(string lecturerId);
    Task<Session> GetSessionForGradingAsync(string sessionId);
    Task GradeEssayAsync(string sessionId, string questionId, decimal score, string? comment);
    Task FinalizeGradingAsync(string sessionId);

    Task<bool> IsGradeLockedAsync(string sessionId);
    Task LockGradeAsync(string sessionId, string actor, string? note = null);
    Task UnlockGradeAsync(string sessionId, string actor, string? note = null);

    Task<bool> HasPendingRegradeRequestAsync(string sessionId);
    Task<string?> GetPendingRegradeReasonAsync(string sessionId);
    Task<List<RegradeRequestItemVm>> GetPendingRegradeRequestsAsync(string lecturerId);
    Task RequestRegradeAsync(string sessionId, string studentId, string reason, string? ipAddress);
    Task ResolveRegradeRequestAsync(string sessionId, string actor, bool approved, string? resolutionNote);

    Task<List<SessionLog>> GetModerationLogsAsync(string sessionId);
}
