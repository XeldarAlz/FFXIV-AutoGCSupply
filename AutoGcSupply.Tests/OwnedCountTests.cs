using AutoGcSupply.Core.Inventory.Model;

namespace AutoGcSupply.Tests;

public sealed class OwnedCountTests
{
    [Fact]
    public void QualitiesAreCountedApart()
    {
        var owned = new OwnedCount(Normal: 2, High: 5);

        Assert.Equal(2, owned.Of(ItemQuality.Normal));
        Assert.Equal(5, owned.Of(ItemQuality.High));
    }

    [Fact]
    public void CoverageNeedsOneQualityToReachTheRequest()
    {
        var split = new OwnedCount(Normal: 2, High: 1);

        Assert.False(split.Covers(ItemQuality.Normal, 3));
        Assert.False(split.Covers(ItemQuality.High, 3));
        Assert.True(split.Covers(ItemQuality.Normal, 2));
        Assert.True(new OwnedCount(Normal: 0, High: 3).Covers(ItemQuality.High, 3));
    }

    [Fact]
    public void ShortfallIsPerQualityAndNeverNegative()
    {
        var owned = new OwnedCount(Normal: 2, High: 0);

        Assert.Equal(1, owned.ShortfallOf(ItemQuality.Normal, 3));
        Assert.Equal(3, owned.ShortfallOf(ItemQuality.High, 3));
        Assert.Equal(0, new OwnedCount(Normal: 9, High: 0).ShortfallOf(ItemQuality.Normal, 3));
    }
}
