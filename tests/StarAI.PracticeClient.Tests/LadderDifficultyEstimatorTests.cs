using StarAI.PracticeClient.Core;

namespace StarAI.PracticeClient.Tests;

public sealed class LadderDifficultyEstimatorTests
{
    [Theory]
    [InlineData(2471, "S")]
    [InlineData(2015, "A")]
    [InlineData(1698, "B")]
    [InlineData(1549, "C")]
    [InlineData(1427, "D")]
    [InlineData(1137, "E")]
    [InlineData(1136, "F")]
    public void GradeForMmrUsesScrReferenceBands(int mmr, string expectedGrade)
    {
        Assert.Equal(expectedGrade, LadderDifficultyEstimator.GradeForMmr(mmr));
    }

    [Fact]
    public void EstimateFromSchnailEloKeepsSourceEloVisible()
    {
        var difficulty = LadderDifficultyEstimator.EstimateFromSchnailElo(1337);

        Assert.NotNull(difficulty);
        Assert.Equal(1337, difficulty.SourceElo);
        Assert.Equal(1337, difficulty.EstimatedMmr);
        Assert.Equal("E", difficulty.Grade);
        Assert.Contains("보정 전", difficulty.Disclaimer);
    }
}
