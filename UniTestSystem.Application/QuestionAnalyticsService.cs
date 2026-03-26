using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class QuestionAnalyticsService : IQuestionAnalyticsService
{
    private readonly IRepository<Session> _sessionRepo;

    public QuestionAnalyticsService(IRepository<Session> sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public async Task<QuestionAnalyticsVm> GetQuestionAnalyticsAsync(DateTime fromUtc, DateTime toUtc, string? courseId = null, int minAttempts = 5)
    {
        if (minAttempts < 1)
            minAttempts = 1;

        var spec = new Specification<Session>(s =>
                s.EndAt.HasValue &&
                s.EndAt.Value >= fromUtc &&
                s.EndAt.Value <= toUtc &&
                s.Status != SessionStatus.NotStarted)
            .Include("Test.TestQuestions")
            .Include("StudentAnswers.Question");

        var sessions = await _sessionRepo.ListAsync(spec);
        if (!string.IsNullOrWhiteSpace(courseId))
        {
            sessions = sessions.Where(s => s.Test?.CourseId == courseId).ToList();
        }

        if (!sessions.Any())
            return new QuestionAnalyticsVm();

        var sessionScoreMap = sessions.ToDictionary(s => s.Id, s => ReportComputationHelper.NormalizeScoreTo10(s), StringComparer.Ordinal);
        var raw = new List<(string SessionId, string QuestionId, decimal Ratio, Question? Question)>();

        foreach (var session in sessions)
        {
            if (session.StudentAnswers == null || session.StudentAnswers.Count == 0)
                continue;

            var testPointMap = session.Test?.TestQuestions?
                .GroupBy(tq => tq.QuestionId)
                .ToDictionary(g => g.Key, g => g.First().Points, StringComparer.Ordinal)
                ?? new Dictionary<string, decimal>(StringComparer.Ordinal);

            var fallbackPoints = session.StudentAnswers.Count > 0
                ? (session.MaxScore > 0 ? session.MaxScore / session.StudentAnswers.Count : 1m)
                : 1m;

            foreach (var answer in session.StudentAnswers)
            {
                if (string.IsNullOrWhiteSpace(answer.QuestionId))
                    continue;

                var points = testPointMap.TryGetValue(answer.QuestionId, out var testPoints) ? testPoints : fallbackPoints;
                if (points <= 0)
                    points = 1m;

                var ratio = Math.Clamp(answer.Score / points, 0m, 1m);
                raw.Add((session.Id, answer.QuestionId, ratio, answer.Question));
            }
        }

        var rows = raw
            .GroupBy(x => x.QuestionId)
            .Select(group =>
            {
                var attempts = group.Count();
                if (attempts < minAttempts)
                    return null;

                var avgRatio = group.Average(x => x.Ratio);
                var avgPercent = Math.Round(avgRatio * 100m, 2);
                var difficultyLabel = ReportComputationHelper.ResolveDifficultyLabel(avgRatio);

                var bySession = group
                    .GroupBy(x => x.SessionId)
                    .Select(x => new
                    {
                        SessionId = x.Key,
                        QuestionRatio = x.Average(v => v.Ratio),
                        TotalScoreNorm = sessionScoreMap.TryGetValue(x.Key, out var score) ? score : 0m
                    })
                    .OrderByDescending(x => x.TotalScoreNorm)
                    .ToList();

                var discriminationIndex = ReportComputationHelper.CalculateDiscriminationIndex(bySession.Select(x => x.QuestionRatio).ToList());
                var discriminationLabel = ReportComputationHelper.ResolveDiscriminationLabel(discriminationIndex);

                var firstQuestion = group.Select(x => x.Question).FirstOrDefault(q => q != null);
                var preview = firstQuestion?.Content ?? "(Missing question content)";
                if (preview.Length > 120)
                    preview = preview[..117] + "...";

                return new QuestionAnalyticsRow
                {
                    QuestionId = group.Key,
                    ContentPreview = preview,
                    Type = firstQuestion?.Type.ToString() ?? "(Unknown)",
                    Subject = firstQuestion?.SubjectId ?? "(Unknown)",
                    Attempts = attempts,
                    AvgScorePercent = avgPercent,
                    DifficultyLabel = difficultyLabel,
                    DiscriminationIndex = Math.Round(discriminationIndex, 3),
                    DiscriminationLabel = discriminationLabel
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .OrderBy(x => x.DiscriminationIndex)
            .ThenBy(x => x.AvgScorePercent)
            .ToList();

        return new QuestionAnalyticsVm
        {
            TotalQuestions = rows.Count,
            HardQuestions = rows.Count(x => x.DifficultyLabel == "Hard"),
            MediumQuestions = rows.Count(x => x.DifficultyLabel == "Medium"),
            EasyQuestions = rows.Count(x => x.DifficultyLabel == "Easy"),
            LowDiscriminationQuestions = rows.Count(x => x.DiscriminationIndex < 0.2m),
            Rows = rows
        };
    }
}
