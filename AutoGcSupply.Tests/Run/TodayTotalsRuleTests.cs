using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class TodayTotalsRuleTests
{
    [Fact]
    public void SumsOnlyIncludedRowsThatAreBoughtOrHandedIn()
    {
        var rows = TodayRows(
            ToBuy(SupplyClasses.Carpenter, CarpenterItem, 1, ItemQuality.High, 1, 10_000, estimatedTotal: 8_000, seals: 800, experience: 200_000),
            Owned(SupplyClasses.Blacksmith, 2, 1, seals: 400, experience: 100_000),
            Row(SupplyClasses.Armorer, 3, 1, RowCoverage.NoListings, ItemQuality.Normal, 1, 0, 0, 0, 90_000, 300),
            Row(SupplyClasses.Goldsmith, 4, 1, RowCoverage.Owned, ItemQuality.Normal, 0, 0, 0, 0, 50_000, 200, included: false),
            ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 10, 263, estimatedTotal: 2_100, seals: 300, experience: 90_000));

        var totals = TodayTotalsRule.Sum(rows, 12_000, 90_000);

        Assert.Equal(new TodayTotals(4, 2, 3, 10_100, 390_000, 1_500, 0), totals);
    }

    [Fact]
    public void SealsOverTheCapAreCountedAsLost()
    {
        var rows = TodayRows(Owned(SupplyClasses.Carpenter, CarpenterItem, 1, seals: 800), Owned(SupplyClasses.Miner, MinerItem, 10, seals: 600));

        var totals = TodayTotalsRule.Sum(rows, 14_500, 15_000);

        Assert.Equal(1_400, totals.Seals);
        Assert.Equal(900, totals.SealsLost);
    }

    [Fact]
    public void AnUnknownSealCapLosesNothing()
    {
        var rows = TodayRows(Owned(SupplyClasses.Carpenter, CarpenterItem, 1, seals: 800));

        Assert.Equal(0, TodayTotalsRule.Sum(rows, 14_500, 0).SealsLost);
    }
}
