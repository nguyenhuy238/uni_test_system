using UniTestSystem.Application.Models;

namespace UniTestSystem.Application.Interfaces;

public interface IQuestionAnalyticsService
{
    Task<QuestionAnalyticsVm> GetQuestionAnalyticsAsync(DateTime fromUtc, DateTime toUtc, string? courseId = null, int minAttempts = 5);
}
