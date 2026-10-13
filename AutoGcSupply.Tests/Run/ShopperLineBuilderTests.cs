using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.Run.RunFixtures;

namespace AutoGcSupply.Tests.Run;

public sealed class ShopperLineBuilderTests
{
    [Fact]
    public void BuildsOneLinePerRowToBuy()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Carpenter, CarpenterItem, 1, ItemQuality.High, 1, 10_000),
            Owned(SupplyClasses.Blacksmith, 2, 1),
            ToBuy(SupplyClasses.Miner, MinerItem, 10, ItemQuality.Normal, 6, 263),
        ];

        var lines = ShopperLineBuilder.Build(plan);

        Assert.Equal(new ShopperLine[]
        {
            new(CarpenterItem, ShopperQuality.HighQuality, 1, 10_000, 0),
            new(MinerItem, ShopperQuality.NormalQuality, 6, 263, 10),
        }, lines);
    }

    [Fact]
    public void MergesRowsOfTheSameItemAndQualityKeepingTheLowerCap()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 2, 1_250),
            ToBuy(SupplyClasses.Alchemist, CulinarianItem, 2, ItemQuality.Normal, 2, 1_100),
            ToBuy(SupplyClasses.Weaver, CulinarianItem, 1, ItemQuality.High, 1, 4_000),
        ];

        var lines = ShopperLineBuilder.Build(plan);

        Assert.Equal(new ShopperLine[]
        {
            new(CulinarianItem, ShopperQuality.NormalQuality, 4, 1_100, 5),
            new(CulinarianItem, ShopperQuality.HighQuality, 1, 4_000, 0),
        }, lines);
    }

    [Fact]
    public void SkipsExcludedAndUnpricedRows()
    {
        RowDecision[] plan =
        [
            ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 1_250) with { Included = false },
            Row(SupplyClasses.Miner, MinerItem, 10, RowCoverage.NoListings, ItemQuality.Normal, 10, 0, 10, 0, 0, 0),
        ];

        Assert.Empty(ShopperLineBuilder.Build(plan));
    }

    [Fact]
    public void KeepsEveryNumberInsideTheBuyingApiLimits()
    {
        RowDecision[] plan = [ToBuy(SupplyClasses.Culinarian, CulinarianItem, 3, ItemQuality.Normal, 3, 5_000_000_000) with { ExcessAllowance = 2_000_000 }];

        var line = Assert.Single(ShopperLineBuilder.Build(plan));

        Assert.Equal(ShopperContract.MaxUnitPrice, line.MaxUnitPrice);
        Assert.Equal(ShopperContract.MaxExcessAllowance, line.ExcessAllowance);
    }
}
