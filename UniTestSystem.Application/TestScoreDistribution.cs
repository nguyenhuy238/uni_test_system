using System.Collections.ObjectModel;

namespace UniTestSystem.Application;

public static class TestScoreDistribution
{
    public const decimal FixedTotalScore = 10m;

    public static IReadOnlyList<decimal> DistributeEvenly(int questionCount, decimal totalScore = FixedTotalScore)
    {
        if (questionCount <= 0 || totalScore <= 0m)
        {
            return Array.Empty<decimal>();
        }

        var safeTotal = Round2(totalScore);
        if (questionCount == 1)
        {
            return new[] { safeTotal };
        }

        var basePoint = Round2(safeTotal / questionCount);
        var values = Enumerable.Repeat(basePoint, questionCount).ToArray();
        values[^1] = Round2(safeTotal - (basePoint * (questionCount - 1)));

        if (values[^1] < 0m)
        {
            // Guard for very large question counts where rounded base may overflow total.
            basePoint = Math.Floor((safeTotal / questionCount) * 100m) / 100m;
            for (var i = 0; i < questionCount; i++)
            {
                values[i] = basePoint;
            }

            values[^1] = Round2(safeTotal - (basePoint * (questionCount - 1)));
        }

        var diff = Round2(safeTotal - values.Sum());
        if (diff != 0m)
        {
            values[^1] = Round2(values[^1] + diff);
        }

        return values;
    }

    public static Dictionary<string, decimal> AllocateEvenlyByQuestionIds(
        IEnumerable<string>? orderedQuestionIds,
        decimal totalScore = FixedTotalScore)
    {
        var ids = NormalizeQuestionIds(orderedQuestionIds);
        var values = DistributeEvenly(ids.Count, totalScore);

        var map = new Dictionary<string, decimal>(StringComparer.Ordinal);
        for (var i = 0; i < ids.Count; i++)
        {
            map[ids[i]] = values[i];
        }

        return map;
    }

    public static Dictionary<string, decimal> NormalizeOrAllocate(
        IEnumerable<string>? orderedQuestionIds,
        IReadOnlyDictionary<string, decimal>? existingPoints,
        decimal totalScore = FixedTotalScore)
    {
        var ids = NormalizeQuestionIds(orderedQuestionIds);
        if (ids.Count == 0)
        {
            return new Dictionary<string, decimal>(StringComparer.Ordinal);
        }

        if (existingPoints != null && IsValidDistribution(ids, existingPoints, totalScore))
        {
            var normalized = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                normalized[id] = Round2(existingPoints[id]);
            }

            return normalized;
        }

        return AllocateEvenlyByQuestionIds(ids, totalScore);
    }

    private static bool IsValidDistribution(
        IReadOnlyList<string> orderedQuestionIds,
        IReadOnlyDictionary<string, decimal> existingPoints,
        decimal totalScore)
    {
        if (orderedQuestionIds.Count == 0)
        {
            return false;
        }

        decimal sum = 0m;
        foreach (var id in orderedQuestionIds)
        {
            if (!existingPoints.TryGetValue(id, out var point))
            {
                return false;
            }

            var rounded = Round2(point);
            if (rounded <= 0m)
            {
                return false;
            }

            sum += rounded;
        }

        var delta = Math.Abs(Round2(sum) - Round2(totalScore));
        return delta == 0m;
    }

    private static IReadOnlyList<string> NormalizeQuestionIds(IEnumerable<string>? orderedQuestionIds)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (orderedQuestionIds == null)
        {
            return Array.Empty<string>();
        }

        foreach (var rawId in orderedQuestionIds)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                continue;
            }

            var id = rawId.Trim();
            if (!seen.Add(id))
            {
                continue;
            }

            ids.Add(id);
        }

        return new ReadOnlyCollection<string>(ids);
    }

    private static decimal Round2(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
