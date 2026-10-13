using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class StartBlockRuleTests
{
    private static readonly StartFacts ready = new(
        Running: false,
        SetupReady: true,
        LoggedIn: true,
        InGrandCompany: true,
        OnHomeWorld: true,
        InDuty: false,
        Loaded: true,
        Pricing: PricingState.Ready,
        Shopper: ShopperState.Available,
        SpendCap: 50_000);

    private static readonly TodayRow[] mixed = TodayRows(
        Owned(SupplyClasses.Carpenter, CarpenterItem, 1),
        ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 10, 263));

    [Fact]
    public void ReadyWhenNothingBlocks()
    {
        Assert.Equal(StartBlock.None, StartBlockRule.Evaluate(ready, mixed));
    }

    [Fact]
    public void BlocksFollowTheEnumPriority()
    {
        var everythingWrong = new StartFacts(true, false, false, false, false, true, false, PricingState.Pending, ShopperState.Missing, 0);

        Assert.Equal(StartBlock.Running, StartBlockRule.Evaluate(everythingWrong, []));
        Assert.Equal(StartBlock.SetupNeeded, StartBlockRule.Evaluate(everythingWrong with { Running = false }, []));
        Assert.Equal(StartBlock.NotLoggedIn, StartBlockRule.Evaluate(everythingWrong with { Running = false, SetupReady = true }, []));
        Assert.Equal(StartBlock.NoGrandCompany, StartBlockRule.Evaluate(ready with { InGrandCompany = false, OnHomeWorld = false, Loaded = false }, []));
        Assert.Equal(StartBlock.NotOnHomeWorld, StartBlockRule.Evaluate(ready with { OnHomeWorld = false, InDuty = true, Loaded = false }, []));
        Assert.Equal(StartBlock.InDuty, StartBlockRule.Evaluate(ready with { InDuty = true, Loaded = false }, []));
        Assert.Equal(StartBlock.NotLoaded, StartBlockRule.Evaluate(ready with { Loaded = false }, []));
        Assert.Equal(StartBlock.NothingSelected, StartBlockRule.Evaluate(ready with { Pricing = PricingState.Pending, SpendCap = 0 }, []));
    }

    [Fact]
    public void ExcludedRowsSelectNothing()
    {
        var rows = TodayRows(Row(SupplyClasses.Carpenter, CarpenterItem, 1, RowCoverage.Excluded, ItemQuality.Normal, 1, 0, 0, 0, 0, 0, included: false));

        Assert.Equal(StartBlock.NothingSelected, StartBlockRule.Evaluate(ready, rows));
    }

    [Fact]
    public void RowsWithoutListingsLeaveNothingToDo()
    {
        var rows = TodayRows(Row(SupplyClasses.Carpenter, CarpenterItem, 1, RowCoverage.NoListings, ItemQuality.Normal, 1, 0, 0, 0, 0, 0));

        Assert.Equal(StartBlock.NothingToDo, StartBlockRule.Evaluate(ready, rows));
    }

    [Theory]
    [InlineData((int)PricingState.Pending, (int)StartBlock.PricesPending)]
    [InlineData((int)PricingState.Failed, (int)StartBlock.PricesFailed)]
    public void UnpricedRowsWaitForTheirPricesOrSayTheyFailed(int pricing, int expected)
    {
        var rows = TodayRows(Owned(SupplyClasses.Carpenter, CarpenterItem, 1), Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0));

        Assert.Equal((StartBlock)expected, StartBlockRule.Evaluate(ready with { Pricing = (PricingState)pricing, SpendCap = 0 }, rows));
    }

    [Fact]
    public void BuyingNeedsASpendCap()
    {
        Assert.Equal(StartBlock.NeedsSpendCap, StartBlockRule.Evaluate(ready with { SpendCap = 0 }, mixed));
    }

    [Fact]
    public void HandingInOwnedItemsNeedsNoSpendCap()
    {
        var rows = TodayRows(Owned(SupplyClasses.Carpenter, CarpenterItem, 1));

        Assert.Equal(StartBlock.None, StartBlockRule.Evaluate(ready with { SpendCap = 0 }, rows));
    }

    [Theory]
    [InlineData((int)ShopperState.Missing)]
    [InlineData((int)ShopperState.Outdated)]
    public void OnlyRowsToBuyWithoutAutoShopperNeedIt(int shopper)
    {
        var rows = TodayRows(Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0));

        Assert.Equal(StartBlock.ShopperNeeded, StartBlockRule.Evaluate(ready with { Shopper = (ShopperState)shopper, Pricing = PricingState.Unavailable }, rows));
    }

    [Fact]
    public void ANewerAutoShopperAsksForAnUpdateOfThisPlugin()
    {
        var rows = TodayRows(Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0));

        Assert.Equal(StartBlock.UpdateNeeded, StartBlockRule.Evaluate(ready with { Shopper = ShopperState.Newer, Pricing = PricingState.Unavailable }, rows));
    }

    [Fact]
    public void WithoutAutoShopperOwnedRowsStillStart()
    {
        var rows = TodayRows(Owned(SupplyClasses.Carpenter, CarpenterItem, 1), Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.Unpriced, ItemQuality.Normal, 10, 0, 0, 0, 0, 0));

        Assert.Equal(StartBlock.None, StartBlockRule.Evaluate(ready with { Shopper = ShopperState.Missing, Pricing = PricingState.Unavailable, SpendCap = 0 }, rows));
    }
}
