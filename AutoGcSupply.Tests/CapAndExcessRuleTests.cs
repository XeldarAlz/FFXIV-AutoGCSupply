using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Tests;

public sealed class CapAndExcessRuleTests
{
    [Theory]
    [InlineData(100, 25, 125)]
    [InlineData(101, 25, 127)]
    [InlineData(1, 25, 2)]
    [InlineData(630, 25, 788)]
    [InlineData(400, 25, 500)]
    [InlineData(100, 0, 100)]
    [InlineData(99, 1, 100)]
    [InlineData(3, 33, 4)]
    [InlineData(1_000, 100, 2_000)]
    [InlineData(210, 25, 263)]
    public void MaxUnitPriceRoundsTheToleranceUp(long unitPrice, int tolerancePercent, long expected)
    {
        Assert.True(CapRule.TryMaxUnitPrice(unitPrice, 0, tolerancePercent, out var maxUnitPrice));
        Assert.Equal(expected, maxUnitPrice);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(100, -1)]
    public void MaxUnitPriceRefusesANonPositivePriceOrNegativeTolerance(long unitPrice, int tolerancePercent)
    {
        Assert.False(CapRule.TryMaxUnitPrice(unitPrice, 0, tolerancePercent, out var maxUnitPrice));
        Assert.Equal(0, maxUnitPrice);
    }

    [Fact]
    public void MaxUnitPriceNeverWrapsAround()
    {
        Assert.False(CapRule.TryMaxUnitPrice(long.MaxValue, 0, 25, out _));
        Assert.False(CapRule.TryMaxUnitPrice(long.MaxValue / 2 + 1, 0, 100, out _));
        Assert.True(CapRule.TryMaxUnitPrice(long.MaxValue / 2, 0, 100, out var doubled));
        Assert.Equal(long.MaxValue - 1, doubled);
        Assert.True(CapRule.TryMaxUnitPrice(long.MaxValue, 0, 0, out var unchanged));
        Assert.Equal(long.MaxValue, unchanged);
    }

    [Theory]
    [InlineData(630, 700, 25, 875)]
    [InlineData(630, 631, 25, 789)]
    [InlineData(630, 630, 25, 788)]
    [InlineData(630, 500, 25, 788)]
    [InlineData(630, 0, 25, 788)]
    [InlineData(100, 300, 0, 300)]
    public void MaxUnitPriceStartsFromTheDearestListingChosen(long unitPrice, long highestUnitPrice, int tolerancePercent, long expected)
    {
        Assert.True(CapRule.TryMaxUnitPrice(unitPrice, highestUnitPrice, tolerancePercent, out var maxUnitPrice));
        Assert.Equal(expected, maxUnitPrice);
    }

    [Fact]
    public void HighestListingDoesNotPriceAQualityWithoutAUnitPrice()
    {
        Assert.False(CapRule.TryMaxUnitPrice(0, 700, 25, out var maxUnitPrice));
        Assert.Equal(0, maxUnitPrice);
    }

    [Fact]
    public void HighestListingNeverWrapsAround()
    {
        Assert.False(CapRule.TryMaxUnitPrice(1, long.MaxValue, 25, out _));
    }

    [Fact]
    public void HugeToleranceDoesNotOverflowThePercentage()
    {
        Assert.True(CapRule.TryMaxUnitPrice(1, 0, int.MaxValue, out var maxUnitPrice));
        Assert.Equal(21_474_838, maxUnitPrice);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    [InlineData(99, 99)]
    public void ExcessAllowanceIsZeroForSingleUnitsElseTheRequestedCount(int requested, int expected)
    {
        Assert.Equal(expected, ExcessRule.Allowance(requested));
    }
}
