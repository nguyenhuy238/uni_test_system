using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IGradingService
{
    Task<List<Session>> GetPendingGradingSessionsAsync(string lecturerId);
    Task<Session> GetSessionForGradingAsync(string sessionId);
    Task GradeEssayAsync(string sessionId, string questionId, decimal score, string? comment);
    Task FinalizeGradingAsync(string sessionId);
}
