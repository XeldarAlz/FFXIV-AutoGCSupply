using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.PriceFixtures;

namespace AutoGcSupply.Tests;

public sealed class RewardMathTests
{
    private static readonly SupplyRequest Weaver = new(6, SupplyClasses.Weaver, 43314, 184_000, 424, 1, 1, false);
    private static readonly SupplyRequest Fisher = new(10, SupplyClasses.Fisher, 4925, 92_000, 302, 5, 5, false);

    [Theory]
    [InlineData(SupplyMissionKind.Supply, ItemQuality.High, 2)]
    [InlineData(SupplyMissionKind.Supply, ItemQuality.Normal, 1)]
    [InlineData(SupplyMissionKind.Provisioning, ItemQuality.High, 1)]
    [InlineData(SupplyMissionKind.Provisioning, ItemQuality.Normal, 1)]
    public void OnlyHighQualitySupplyDoubles(SupplyMissionKind kind, ItemQuality quality, int factor)
    {
        Assert.Equal(factor, RewardMath.Factor(kind, quality));
    }

    [Fact]
    public void HighQualitySupplyDoublesExperienceAndSeals()
    {
        Assert.Equal(368_000, RewardMath.Experience(Weaver, ItemQuality.High));
        Assert.Equal(848, RewardMath.Seals(Weaver, ItemQuality.High));
        Assert.Equal(184_000, RewardMath.Experience(Weaver, ItemQuality.Normal));
        Assert.Equal(424, RewardMath.Seals(Weaver, ItemQuality.Normal));
    }

    [Fact]
    public void ProvisioningNeverDoubles()
    {
        Assert.Equal(92_000, RewardMath.Experience(Fisher, ItemQuality.High));
        Assert.Equal(302, RewardMath.Seals(Fisher, ItemQuality.High));
    }

    [Fact]
    public void LargestRewardsDoubleWithoutOverflow()
    {
        var largest = Weaver with { ExperienceReward = uint.MaxValue, SealReward = uint.MaxValue };

        Assert.Equal(2L * uint.MaxValue, RewardMath.Experience(largest, ItemQuality.High));
        Assert.Equal(2L * uint.MaxValue, RewardMath.Seals(largest, ItemQuality.High));
    }

    [Theory]
    [InlineData(250_000, 1_000_000, 0.25)]
    [InlineData(1_500_000, 1_000_000, 1.5)]
    [InlineData(0, 1_000_000, 0)]
    [InlineData(250_000, 0, 0)]
    [InlineData(250_000, -5, 0)]
    public void LevelShareDividesByTheExperienceToNextLevel(long experience, int experienceToNext, double expected)
    {
        Assert.Equal(expected, RewardMath.LevelShare(experience, experienceToNext), 10);
    }

    [Theory]
    [InlineData(1_890, 6_000, 315)]
    [InlineData(1_000, 3_000, 333)]
    [InlineData(2_000, 3_000, 667)]
    [InlineData(500, 1_000, 500)]
    [InlineData(0, 1_000, 0)]
    [InlineData(20_000, 368_000, 54)]
    public void GilPerThousandExperienceRoundsToTheNearestGil(long gil, long experience, long expected)
    {
        Assert.True(RewardMath.TryGilPerThousandExperience(gil, experience, out var gilPerThousand));
        Assert.Equal(expected, gilPerThousand);
    }

    [Theory]
    [InlineData(1_000, 0)]
    [InlineData(1_000, -1)]
    [InlineData(-1, 1_000)]
    [InlineData(long.MaxValue, 1)]
    public void GilPerThousandExperienceRefusesWhatCannotBeComputed(long gil, long experience)
    {
        Assert.False(RewardMath.TryGilPerThousandExperience(gil, experience, out var gilPerThousand));
        Assert.Equal(0, gilPerThousand);
    }

    [Fact]
    public void GilPerThousandExperienceUsesWideArithmetic()
    {
        Assert.True(RewardMath.TryGilPerThousandExperience(long.MaxValue, 1_000, out var gilPerThousand));
        Assert.Equal(long.MaxValue, gilPerThousand);
    }

    [Theory]
    [InlineData(8_000, 10_000, 1_500, 0)]
    [InlineData(9_000, 10_000, 1_000, 0)]
    [InlineData(9_000, 10_000, 1_500, 500)]
    [InlineData(10_000, 10_000, 1_500, 1_500)]
    [InlineData(12_000, 10_000, 1_500, 1_500)]
    [InlineData(9_000, 10_000, 0, 0)]
    public void SealsOverTheCapAreLost(long currentSeals, long sealCap, long reward, long expected)
    {
        Assert.Equal(expected, RewardMath.SealsLost(currentSeals, sealCap, reward));
    }

    [Fact]
    public void EstimateUsesTheQuotedTotalOnlyForTheQuotedQuantity()
    {
        var price = Item(5106, 10, Listed(12, 2_520), None);

        Assert.True(price.TryEstimate(ItemQuality.Normal, 10, out var quoted));
        Assert.Equal(2_520, quoted);
        Assert.True(price.TryEstimate(ItemQuality.Normal, 4, out var partial));
        Assert.Equal(840, partial);
    }

    [Fact]
    public void EstimateRefusesUnusablePricesAndEmptyShortfalls()
    {
        var price = Item(5106, 10, Listed(10, 2_100), None);

        Assert.False(price.TryEstimate(ItemQuality.High, 10, out _));
        Assert.False(price.TryEstimate(ItemQuality.Normal, 0, out _));
        Assert.False(Item(5106, 10, new QualityPrice(true, 0, 2_100, 210), None).TryEstimate(ItemQuality.Normal, 10, out _));
        Assert.False(Item(5106, 10, new QualityPrice(true, 10, long.MaxValue, long.MaxValue / 2), None).TryEstimate(ItemQuality.Normal, 3, out _));
    }

    [Fact]
    public void PriceOfPicksTheQuality()
    {
        var normal = Listed(1, 100);
        var high = Listed(1, 300);
        var price = Item(43314, 1, normal, high);

        Assert.Equal(normal, price.Of(ItemQuality.Normal));
        Assert.Equal(high, price.Of(ItemQuality.High));
    }
}
