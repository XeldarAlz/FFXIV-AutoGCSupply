using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Tests.Run;

public sealed class SpendCapRuleTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    [InlineData(10, 100)]
    [InlineData(1_234, 1_500)]
    [InlineData(1_250, 1_500)]
    [InlineData(8_333, 10_000)]
    [InlineData(12_345, 15_000)]
    [InlineData(83_333, 100_000)]
    [InlineData(123_456, 150_000)]
    [InlineData(2_500_000, 3_000_000)]
    public void SuggestsTheEstimatePlusAFifthRoundedUpToTwoFigures(long estimate, long expected)
    {
        Assert.Equal(expected, SpendCapRule.Suggest(estimate));
    }

    [Theory]
    [InlineData(900_000_000)]
    [InlineData(999_999_999)]
    [InlineData(long.MaxValue)]
    public void SuggestionNeverPassesTheMaximum(long estimate)
    {
        Assert.Equal(SpendCapRule.Maximum, SpendCapRule.Suggest(estimate));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(50_000, 50_000)]
    [InlineData(long.MaxValue, SpendCapRule.Maximum)]
    public void ClampKeepsTheCapInsideTheBuyingApiLimits(long gil, long expected)
    {
        Assert.Equal(expected, SpendCapRule.Clamp(gil));
    }
}
