using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class HandInTargetTests
{
    [Theory]
    [InlineData(8, 0)]
    [InlineData(15, 0)]
    [InlineData(16, 1)]
    [InlineData(18, 1)]
    public void PlacesEachClassOnItsTab(int classJobId, int expectedTab)
    {
        var target = new HandInTarget(2_001, (byte)classJobId, 1, HandInQuality.Normal, 100);

        Assert.Equal((SupplyListTab)expectedTab, target.Tab);
        Assert.True(target.IsValid);
    }

    [Fact]
    public void AllowsHighQualityOnlyForSupply()
    {
        Assert.True(new HandInTarget(2_001, 8, 1, HandInQuality.High, 100).IsValid);
        Assert.False(new HandInTarget(2_004, 17, 10, HandInQuality.High, 100).IsValid);
    }

    [Fact]
    public void RefusesANegativeSealReward()
    {
        Assert.False(new HandInTarget(2_001, 8, 1, HandInQuality.Normal, -1).IsValid);
    }
}
