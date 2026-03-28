using UniTestSystem.Application;

namespace UniTestSystem.Application.Tests;

public class TestScoreDistributionTests
{
    [Fact]
    public void DistributeEvenly_N1_ReturnsSingleTenPointQuestion()
    {
        var scores = TestScoreDistribution.DistributeEvenly(1);

        Assert.Equal(new[] { 10m }, scores);
    }

    [Fact]
    public void DistributeEvenly_N3_AssignsRemainderToLastQuestion()
    {
        var scores = TestScoreDistribution.DistributeEvenly(3);

        Assert.Equal(new[] { 3.33m, 3.33m, 3.34m }, scores);
        Assert.Equal(10m, scores.Sum());
    }

    [Fact]
    public void DistributeEvenly_N6_AssignsRemainderToLastQuestion()
    {
        var scores = TestScoreDistribution.DistributeEvenly(6);

        Assert.Equal(new[] { 1.67m, 1.67m, 1.67m, 1.67m, 1.67m, 1.65m }, scores);
        Assert.Equal(10m, scores.Sum());
    }

    [Fact]
    public void DistributeEvenly_N7_AssignsRemainderToLastQuestion()
    {
        var scores = TestScoreDistribution.DistributeEvenly(7);

        Assert.Equal(new[] { 1.43m, 1.43m, 1.43m, 1.43m, 1.43m, 1.43m, 1.42m }, scores);
        Assert.Equal(10m, scores.Sum());
    }

    [Fact]
    public void DistributeEvenly_N13_AssignsRemainderToLastQuestion()
    {
        var scores = TestScoreDistribution.DistributeEvenly(13);

        Assert.Equal(13, scores.Count);
        Assert.All(scores.Take(12), x => Assert.Equal(0.77m, x));
        Assert.Equal(0.76m, scores[^1]);
        Assert.Equal(10m, scores.Sum());
    }

    [Fact]
    public void NormalizeOrAllocate_InvalidLegacyMap_RebalancesToFixedTotal()
    {
        var ids = new[] { "q1", "q2", "q3", "q4", "q5", "q6" };
        var legacy = ids.ToDictionary(id => id, _ => 1m, StringComparer.Ordinal);

        var normalized = TestScoreDistribution.NormalizeOrAllocate(ids, legacy);

        Assert.Equal(6, normalized.Count);
        Assert.Equal(10m, normalized.Values.Sum());
        Assert.Equal(1.65m, normalized["q6"]);
    }
}
