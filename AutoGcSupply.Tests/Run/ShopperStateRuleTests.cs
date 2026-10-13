using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Tests.Run;

public sealed class ShopperStateRuleTests
{
    [Fact]
    public void TheContractVersionWithEveryGateIsAvailable()
    {
        Assert.Equal(ShopperState.Available, ShopperStateRule.Classify(installed: true, gatesPresent: true, apiVersion: 1));
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void AnInstalledAutoShopperWithoutTheApiIsOutdated(bool gatesPresent, int apiVersion)
    {
        Assert.Equal(ShopperState.Outdated, ShopperStateRule.Classify(installed: true, gatesPresent, apiVersion));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ANewerApiAsksForAnUpdateOfThisPlugin(bool installed, bool gatesPresent)
    {
        Assert.Equal(ShopperState.Newer, ShopperStateRule.Classify(installed, gatesPresent, apiVersion: 2));
    }

    [Fact]
    public void NothingInstalledAndNoAnswerIsMissing()
    {
        Assert.Equal(ShopperState.Missing, ShopperStateRule.Classify(installed: false, gatesPresent: false, apiVersion: 0));
    }
}
