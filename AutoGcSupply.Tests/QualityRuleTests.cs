using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.PriceFixtures;

namespace AutoGcSupply.Tests;

public sealed class QualityRuleTests
{
    private const uint ItemId = 43314;

    [Fact]
    public void HighCostingExactlyTwiceNormalBuysHigh()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, Listed(1, 1_000), Listed(1, 2_000)));

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.High, 1, 2_000, 2_000), choice);
    }

    [Fact]
    public void HighCostingOneGilMoreThanTwiceNormalBuysNormal()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, Listed(1, 1_000), Listed(1, 2_001)));

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 1, 1_000, 1_000), choice);
    }

    [Fact]
    public void HighCheaperThanNormalBuysHigh()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, Listed(1, 1_000), Listed(1, 900)));

        Assert.Equal(ItemQuality.High, choice.Quality);
    }

    [Fact]
    public void MissingHighBuysNormal()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, Listed(1, 1_000), None));

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 1, 1_000, 1_000), choice);
    }

    [Fact]
    public void MissingNormalBuysHighWhateverItCosts()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, None, Listed(1, 50_000)));

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.High, 1, 50_000, 50_000), choice);
    }

    [Fact]
    public void NoListingForEitherQualityIsNoListings()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, None, None));

        Assert.Equal(new QualityChoice(RowCoverage.NoListings, ItemQuality.Normal, 1, 0, 0), choice);
    }

    [Fact]
    public void MissingQuoteIsUnpriced()
    {
        var choice = QualityRule.Choose(SupplyMissionKind.Supply, SupplyQualityPolicy.Automatic, 3, new OwnedCount(1, 0), false, default);

        Assert.Equal(new QualityChoice(RowCoverage.Unpriced, ItemQuality.Normal, 2, 0, 0), choice);
    }

    [Fact]
    public void OwnedNormalCoversEvenWhenHighWouldPayMore()
    {
        var choice = Supply(new OwnedCount(1, 0), Item(ItemId, 1, Listed(1, 100_000), Listed(1, 10)));

        Assert.Equal(new QualityChoice(RowCoverage.Owned, ItemQuality.Normal, 0, 0, 0), choice);
    }

    [Fact]
    public void OwnedHighCovers()
    {
        var choice = Supply(new OwnedCount(0, 1), Item(ItemId, 1, Listed(1, 10), None));

        Assert.Equal(new QualityChoice(RowCoverage.Owned, ItemQuality.High, 0, 0, 0), choice);
    }

    [Fact]
    public void BothQualitiesOwnedHandsInHighForTheDoubleReward()
    {
        var choice = Supply(new OwnedCount(1, 1), Item(ItemId, 1, None, None));

        Assert.Equal(new QualityChoice(RowCoverage.Owned, ItemQuality.High, 0, 0, 0), choice);
    }

    [Fact]
    public void OwnedSplitAcrossQualitiesIsNeverCombined()
    {
        var price = Item(ItemId, 3, PerUnit(3, 100), PerUnit(3, 200));

        var choice = Supply(new OwnedCount(2, 1), price, requested: 3);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 1, 100, 100), choice);
    }

    [Fact]
    public void EachQualityIsPricedForItsOwnShortfall()
    {
        var price = Item(ItemId, 3, PerUnit(3, 100), PerUnit(3, 70));

        var choice = Supply(new OwnedCount(2, 0), price, requested: 3);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 1, 100, 100), choice);
    }

    [Fact]
    public void HighShortfallIsNotTakenFromTheNormalStock()
    {
        var price = Item(ItemId, 3, PerUnit(3, 100), Listed(3, 200));

        var choice = Supply(new OwnedCount(2, 0), price, requested: 3);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.High, 3, 67, 200), choice);
    }

    [Fact]
    public void ShortfallEqualToTheQuoteUsesTheQuotedTotal()
    {
        var stackOfFive = Listed(5, 525);

        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 3, stackOfFive, None), requested: 3);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 3, 105, 525), choice);
    }

    [Fact]
    public void AlwaysHighIgnoresOwnedNormal()
    {
        var choice = Supply(new OwnedCount(1, 0), Item(ItemId, 1, Listed(1, 10), Listed(1, 5_000)), SupplyQualityPolicy.AlwaysHigh);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.High, 1, 5_000, 5_000), choice);
    }

    [Fact]
    public void AlwaysHighUsesOwnedHigh()
    {
        var choice = Supply(new OwnedCount(0, 1), Item(ItemId, 1, Listed(1, 10), None), SupplyQualityPolicy.AlwaysHigh);

        Assert.Equal(new QualityChoice(RowCoverage.Owned, ItemQuality.High, 0, 0, 0), choice);
    }

    [Fact]
    public void AlwaysHighWithoutHighListingsIsNoListings()
    {
        var choice = Supply(new OwnedCount(1, 0), Item(ItemId, 1, Listed(1, 10), None), SupplyQualityPolicy.AlwaysHigh);

        Assert.Equal(new QualityChoice(RowCoverage.NoListings, ItemQuality.High, 1, 0, 0), choice);
    }

    [Fact]
    public void AlwaysHighUnpricedKeepsHigh()
    {
        var choice = QualityRule.Choose(SupplyMissionKind.Supply, SupplyQualityPolicy.AlwaysHigh, 1, new OwnedCount(1, 0), false, default);

        Assert.Equal(new QualityChoice(RowCoverage.Unpriced, ItemQuality.High, 1, 0, 0), choice);
    }

    [Fact]
    public void AlwaysNormalIgnoresOwnedAndCheaperHigh()
    {
        var choice = Supply(new OwnedCount(0, 1), Item(ItemId, 1, Listed(1, 1_000), Listed(1, 100)), SupplyQualityPolicy.AlwaysNormal);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 1, 1_000, 1_000), choice);
    }

    [Fact]
    public void AlwaysNormalWithoutNormalListingsIsNoListings()
    {
        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, None, Listed(1, 100)), SupplyQualityPolicy.AlwaysNormal);

        Assert.Equal(new QualityChoice(RowCoverage.NoListings, ItemQuality.Normal, 1, 0, 0), choice);
    }

    [Fact]
    public void ProvisioningPrefersNormalEvenWhenHighIsCheaper()
    {
        var choice = Provisioning(new OwnedCount(0, 0), Item(ItemId, 10, PerUnit(10, 100), PerUnit(10, 50)));

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 10, 100, 1_000), choice);
    }

    [Fact]
    public void ProvisioningBuysHighOnlyWithoutNormalListings()
    {
        var choice = Provisioning(new OwnedCount(0, 0), Item(ItemId, 10, None, PerUnit(10, 400)));

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.High, 10, 400, 4_000), choice);
    }

    [Theory]
    [InlineData(SupplyQualityPolicy.AlwaysHigh)]
    [InlineData(SupplyQualityPolicy.AlwaysNormal)]
    [InlineData(SupplyQualityPolicy.Automatic)]
    public void ProvisioningIgnoresTheSupplyPolicy(SupplyQualityPolicy policy)
    {
        var onlyNormal = QualityRule.Choose(SupplyMissionKind.Provisioning, policy, 10, default, true, Item(ItemId, 10, PerUnit(10, 100), None));
        var onlyHigh = QualityRule.Choose(SupplyMissionKind.Provisioning, policy, 10, default, true, Item(ItemId, 10, None, PerUnit(10, 100)));

        Assert.Equal(ItemQuality.Normal, onlyNormal.Quality);
        Assert.Equal(RowCoverage.ToBuy, onlyNormal.Coverage);
        Assert.Equal(ItemQuality.High, onlyHigh.Quality);
        Assert.Equal(RowCoverage.ToBuy, onlyHigh.Coverage);
    }

    [Fact]
    public void ProvisioningUsesOwnedNormalBeforeOwnedHigh()
    {
        var both = Provisioning(new OwnedCount(10, 10), Item(ItemId, 10, None, None));
        var onlyHigh = Provisioning(new OwnedCount(3, 10), Item(ItemId, 10, None, None));

        Assert.Equal(new QualityChoice(RowCoverage.Owned, ItemQuality.Normal, 0, 0, 0), both);
        Assert.Equal(new QualityChoice(RowCoverage.Owned, ItemQuality.High, 0, 0, 0), onlyHigh);
    }

    [Fact]
    public void AvailableQuoteWithoutAPriceIsNotUsable()
    {
        var broken = new QualityPrice(true, 1, 0, 0);

        var choice = Supply(new OwnedCount(0, 0), Item(ItemId, 1, broken, broken));

        Assert.Equal(RowCoverage.NoListings, choice.Coverage);
    }

    [Fact]
    public void EstimateOverflowDropsThatQuality()
    {
        var price = Item(ItemId, 3, PerUnit(3, 100), new QualityPrice(true, 3, long.MaxValue, long.MaxValue));

        var choice = Supply(new OwnedCount(0, 1), price, requested: 3);

        Assert.Equal(new QualityChoice(RowCoverage.ToBuy, ItemQuality.Normal, 3, 100, 300), choice);
    }

    [Fact]
    public void HighPaysOffComparesWithoutOverflow()
    {
        Assert.True(QualityRule.HighPaysOff(SupplyMissionKind.Supply, long.MaxValue, long.MaxValue));
        Assert.True(QualityRule.HighPaysOff(SupplyMissionKind.Supply, long.MaxValue, long.MaxValue / 2 + 1));
        Assert.False(QualityRule.HighPaysOff(SupplyMissionKind.Supply, long.MaxValue, long.MaxValue / 2));
        Assert.False(QualityRule.HighPaysOff(SupplyMissionKind.Provisioning, 1, 1_000));
    }

    private static QualityChoice Supply(OwnedCount owned, ItemPrice price, SupplyQualityPolicy policy = SupplyQualityPolicy.Automatic, int requested = 1)
        => QualityRule.Choose(SupplyMissionKind.Supply, policy, requested, owned, true, price);

    private static QualityChoice Provisioning(OwnedCount owned, ItemPrice price, int requested = 10)
        => QualityRule.Choose(SupplyMissionKind.Provisioning, SupplyQualityPolicy.Automatic, requested, owned, true, price);
}
