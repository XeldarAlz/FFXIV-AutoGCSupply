using AutoGcSupply.Core.Tasks.Model;

namespace AutoGcSupply.Tests.Run;

public sealed class ShopperStateRuleTests
{
    [Fact]
    public void TheContractVersionWithEveryGateIsAvailable()
    {
        Assert.Equal(ShopperState.Available, ShopperStateRule.Classify(installed: true, gatesMatch: true, apiVersion: 1));
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    public void AnInstalledAutoShopperWithoutTheApiIsOutdated(bool gatesMatch, int apiVersion)
    {
        Assert.Equal(ShopperState.Outdated, ShopperStateRule.Classify(installed: true, gatesMatch, apiVersion));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ANewerApiAsksForAnUpdateOfThisPlugin(bool installed, bool gatesMatch)
    {
        Assert.Equal(ShopperState.Newer, ShopperStateRule.Classify(installed, gatesMatch, apiVersion: 2));
    }

    [Fact]
    public void NothingInstalledAndNoAnswerIsMissing()
    {
        Assert.Equal(ShopperState.Missing, ShopperStateRule.Classify(installed: false, gatesMatch: false, apiVersion: 0));
    }
}
