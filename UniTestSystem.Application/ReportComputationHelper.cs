using UniTestSystem.Domain;

namespace UniTestSystem.Application;

internal static class ReportComputationHelper
{
    public static decimal NormalizeScoreTo10(Session session)
    {
        if (session.MaxScore <= 0)
            return Math.Round(Math.Clamp(session.TotalScore, 0m, 10m), 2);

        var normalized = (session.TotalScore / session.MaxScore) * 10m;
        return Math.Round(Math.Clamp(normalized, 0m, 10m), 2);
    }

    public static string ResolveDifficultyLabel(decimal avgRatio)
    {
        if (avgRatio < 0.4m) return "Hard";
        if (avgRatio < 0.7m) return "Medium";
        return "Easy";
    }

    public static decimal CalculateDiscriminationIndex(List<decimal> orderedQuestionRatiosBySessionScore)
    {
        if (orderedQuestionRatiosBySessionScore.Count < 3)
            return 0m;

        var n = Math.Max(1, (int)Math.Round(orderedQuestionRatiosBySessionScore.Count * 0.27m, MidpointRounding.AwayFromZero));
        var upper = orderedQuestionRatiosBySessionScore.Take(n).ToList();
        var lower = orderedQuestionRatiosBySessionScore
            .Skip(Math.Max(0, orderedQuestionRatiosBySessionScore.Count - n))
            .Take(n)
            .ToList();

        if (!upper.Any() || !lower.Any())
            return 0m;

        return upper.Average() - lower.Average();
    }

    public static string ResolveDiscriminationLabel(decimal discriminationIndex)
    {
        if (discriminationIndex >= 0.4m) return "Excellent";
        if (discriminationIndex >= 0.2m) return "Good";
        if (discriminationIndex >= 0m) return "Weak";
        return "Inverse";
    }

    public static string ResolveSubjectLabel(Session session)
    {
        if (!string.IsNullOrWhiteSpace(session.Test?.Course?.Name))
            return session.Test.Course.Name.Trim();
        if (!string.IsNullOrWhiteSpace(session.Test?.Title))
            return session.Test.Title.Trim();
        return "(Unknown Subject)";
    }

    public static string ResolveSemesterLabel(Session session, IReadOnlyDictionary<string, string> enrollmentSemesterMap)
    {
        if (!string.IsNullOrWhiteSpace(session.Test?.Course?.Semester))
            return session.Test.Course.Semester.Trim();

        if (!string.IsNullOrWhiteSpace(session.Test?.CourseId))
        {
            var key = $"{session.UserId}|{session.Test.CourseId}";
            if (enrollmentSemesterMap.TryGetValue(key, out var semester) && !string.IsNullOrWhiteSpace(semester))
                return semester.Trim();
        }

        return "(Unassigned Semester)";
    }
}
