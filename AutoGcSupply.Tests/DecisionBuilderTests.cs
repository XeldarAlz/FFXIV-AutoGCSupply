using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Value.Model;
using static AutoGcSupply.Tests.PriceFixtures;

namespace AutoGcSupply.Tests;

public sealed class DecisionBuilderTests
{
    private const int Tolerance = 25;
    private const int BlacksmithSlot = 7;

    private static readonly DecisionSettings Defaults = new(
        IncludeMaxLevelClasses: false,
        UseOwnedItems: true,
        SupplyQuality: SupplyQualityPolicy.Automatic,
        PriceTolerancePercent: Tolerance,
        ExcludedClasses: ClassMask.None.With(SupplyClasses.Armorer, true));

    [Fact]
    public void DecidesEveryRowOfAFullDay()
    {
        var rows = Build(Defaults);

        Assert.Equal(SupplyClasses.Count, rows.Length);
        Assert.Equal((SupplyClasses.Carpenter, RowCoverage.MaxLevel, false, ItemQuality.High, 1, 8_000L, 8_000L, 10_000L, 0, 0L, 840L, 0L), Summary(rows[0]));
        Assert.Equal((SupplyClasses.Blacksmith, RowCoverage.Delivered, false, ItemQuality.Normal, 0, 0L, 0L, 0L, 0, 0L, 0L, 0L), Summary(rows[1]));
        Assert.Equal((SupplyClasses.Armorer, RowCoverage.Excluded, false, ItemQuality.Normal, 1, 3_000L, 3_000L, 3_750L, 0, 181_000L, 421L, 17L), Summary(rows[2]));
        Assert.Equal((SupplyClasses.Goldsmith, RowCoverage.Owned, true, ItemQuality.Normal, 0, 0L, 0L, 0L, 0, 183_000L, 423L, 0L), Summary(rows[3]));
        Assert.Equal((SupplyClasses.Leatherworker, RowCoverage.Owned, true, ItemQuality.High, 0, 0L, 0L, 0L, 0, 364_000L, 844L, 0L), Summary(rows[4]));
        Assert.Equal((SupplyClasses.Weaver, RowCoverage.ToBuy, true, ItemQuality.High, 1, 20_000L, 20_000L, 25_000L, 0, 368_000L, 848L, 54L), Summary(rows[5]));
        Assert.Equal((SupplyClasses.Alchemist, RowCoverage.ToBuy, true, ItemQuality.Normal, 2, 1_000L, 2_000L, 1_250L, 3, 176_000L, 412L, 11L), Summary(rows[6]));
        Assert.Equal((SupplyClasses.Culinarian, RowCoverage.Unpriced, true, ItemQuality.Normal, 3, 0L, 0L, 0L, 0, 175_000L, 410L, 0L), Summary(rows[7]));
        Assert.Equal((SupplyClasses.Miner, RowCoverage.ToBuy, true, ItemQuality.Normal, 10, 210L, 2_100L, 263L, 10, 90_000L, 300L, 23L), Summary(rows[8]));
        Assert.Equal((SupplyClasses.Botanist, RowCoverage.NoListings, true, ItemQuality.Normal, 20, 0L, 0L, 0L, 0, 91_000L, 301L, 0L), Summary(rows[9]));
        Assert.Equal((SupplyClasses.Fisher, RowCoverage.Owned, true, ItemQuality.Normal, 0, 0L, 0L, 0L, 0, 92_000L, 302L, 0L), Summary(rows[10]));
    }

    [Fact]
    public void RowsComeOutInDisplayOrderWhateverTheMemoryOrder()
    {
        var rows = Build(Defaults);

        for (var index = 0; index < rows.Length; index++)
        {
            Assert.Equal(SupplyClasses.DisplayOrder[index], rows[index].Request.ClassJobId);
        }

        Assert.Equal(0, rows[0].Request.Slot);
        Assert.Equal(1, rows[7].Request.Slot);
        Assert.Equal(BlacksmithSlot, rows[1].Request.Slot);
    }

    [Fact]
    public void LevelShareComesFromTheClassExperienceToNext()
    {
        var rows = Build(Defaults);

        Assert.Equal(183_000d / CharacterFixtures.ExperienceToNext, rows[3].LevelShare);
        Assert.Equal(368_000d / CharacterFixtures.ExperienceToNext, rows[5].LevelShare);
        Assert.Equal(0d, rows[0].LevelShare);
        Assert.Equal(0d, rows[1].LevelShare);
    }

    [Fact]
    public void OwnedCountsAreReportedOnEveryRow()
    {
        var rows = Build(Defaults);

        Assert.Equal(new OwnedCount(1, 0), rows[1].Owned);
        Assert.Equal(new OwnedCount(1, 0), rows[6].Owned);
        Assert.Equal(new OwnedCount(0, 1), rows[4].Owned);
        Assert.Equal(new OwnedCount(0, 0), rows[5].Owned);
    }

    [Fact]
    public void PurchaseAndHandInFlagsFollowInclusionAndCoverage()
    {
        var rows = Build(Defaults);

        Assert.Equal(new[] { false, false, false, false, false, true, true, false, true, false, false }, Flags(rows, static row => row.NeedsPurchase));
        Assert.Equal(new[] { false, false, false, true, true, false, false, false, false, false, true }, Flags(rows, static row => row.ReadyToHandIn));
    }

    [Fact]
    public void MaxLevelClassJoinsForSealsOnlyWhenAllowed()
    {
        var rows = Build(Defaults with { IncludeMaxLevelClasses = true });

        Assert.Equal((SupplyClasses.Carpenter, RowCoverage.ToBuy, true, ItemQuality.High, 1, 8_000L, 8_000L, 10_000L, 0, 0L, 840L, 0L), Summary(rows[0]));
        Assert.True(rows[0].NeedsPurchase);
    }

    [Fact]
    public void ExclusionWinsOverMaxLevel()
    {
        var settings = Defaults with { IncludeMaxLevelClasses = true, ExcludedClasses = Defaults.ExcludedClasses.With(SupplyClasses.Carpenter, true) };

        var rows = Build(settings);

        Assert.Equal(RowCoverage.Excluded, rows[0].Coverage);
        Assert.False(rows[0].Included);
        Assert.Equal(RowCoverage.Excluded, Build(Defaults with { ExcludedClasses = settings.ExcludedClasses })[0].Coverage);
    }

    [Fact]
    public void NotUsingOwnedItemsBuysTheWholeRequest()
    {
        var prices = new List<ItemPrice>(Prices()) { Item(43341, 1, Listed(1, 700), None) };

        var rows = Build(Defaults with { UseOwnedItems = false }, prices.ToArray());

        Assert.Equal((SupplyClasses.Goldsmith, RowCoverage.ToBuy, true, ItemQuality.Normal, 1, 700L, 700L, 875L, 0, 183_000L, 423L, 4L), Summary(rows[3]));
        Assert.Equal((SupplyClasses.Alchemist, RowCoverage.ToBuy, true, ItemQuality.Normal, 3, 1_000L, 3_000L, 1_250L, 3, 176_000L, 412L, 17L), Summary(rows[6]));
        Assert.Equal(RowCoverage.Unpriced, rows[4].Coverage);
        Assert.Equal((RowCoverage.Unpriced, 5), (rows[10].Coverage, rows[10].BuyQuantity));
        Assert.Equal(new OwnedCount(1, 0), rows[3].Owned);
    }

    [Fact]
    public void AlwaysHighChangesSupplyRowsOnly()
    {
        var rows = Build(Defaults with { SupplyQuality = SupplyQualityPolicy.AlwaysHigh });

        Assert.Equal((SupplyClasses.Goldsmith, RowCoverage.Unpriced, true, ItemQuality.High, 1, 0L, 0L, 0L, 0, 366_000L, 846L, 0L), Summary(rows[3]));
        Assert.Equal((RowCoverage.Owned, ItemQuality.High), (rows[4].Coverage, rows[4].ChosenQuality));
        Assert.Equal((SupplyClasses.Alchemist, RowCoverage.ToBuy, true, ItemQuality.High, 3, 3_000L, 9_000L, 3_750L, 3, 352_000L, 824L, 26L), Summary(rows[6]));
        Assert.Equal((RowCoverage.ToBuy, ItemQuality.Normal, 90_000L), (rows[8].Coverage, rows[8].ChosenQuality, rows[8].Experience));
        Assert.Equal((RowCoverage.Owned, ItemQuality.Normal), (rows[10].Coverage, rows[10].ChosenQuality));
    }

    [Fact]
    public void CapThatCannotBeComputedLeavesTheRowUnpriced()
    {
        var prices = Prices();
        prices[2] = Item(43314, 1, new QualityPrice(true, 1, long.MaxValue, long.MaxValue), None);

        var rows = Build(Defaults, prices);

        Assert.Equal((SupplyClasses.Weaver, RowCoverage.Unpriced, true, ItemQuality.Normal, 1, 0L, 0L, 0L, 0, 184_000L, 424L, 0L), Summary(rows[5]));
        Assert.False(rows[5].NeedsPurchase);
    }

    [Fact]
    public void ZeroToleranceCapsAtTheQuotedUnitPrice()
    {
        var rows = Build(Defaults with { PriceTolerancePercent = 0 });

        Assert.Equal(20_000, rows[5].MaxUnitPrice);
        Assert.Equal(1_000, rows[6].MaxUnitPrice);
        Assert.Equal(210, rows[8].MaxUnitPrice);
    }

    [Fact]
    public void WithoutQuotesOnlyOwnedRowsAreReady()
    {
        var rows = Build(Defaults, []);

        Assert.Equal(
            new[] { RowCoverage.MaxLevel, RowCoverage.Delivered, RowCoverage.Excluded, RowCoverage.Owned, RowCoverage.Owned, RowCoverage.Unpriced, RowCoverage.Unpriced, RowCoverage.Unpriced, RowCoverage.Unpriced, RowCoverage.Unpriced, RowCoverage.Owned },
            Coverages(rows));
        Assert.Equal(0L, rows[5].EstimatedTotal);
    }

    [Fact]
    public void StarredRowsCarryTheBadgeWithoutABonus()
    {
        var rows = Build(Defaults);

        Assert.True(rows[7].Request.Starred);
        Assert.True(rows[9].Request.Starred);
        Assert.Equal(410, rows[7].Seals);
        Assert.Equal(301, rows[9].Seals);
    }

    [Fact]
    public void UnloadedOrInvalidSnapshotBuildsNothing()
    {
        var unloaded = RequestSnapshot.Create(
            SupplyFixtures.ContentId,
            SupplyFixtures.ReadAtUtc,
            SnapshotParser.Parse(new byte[SnapshotParser.TotalBytes]),
            SupplyFixtures.AnyItemExists);
        var duplicate = SupplyFixtures.OpenDayCopy();
        duplicate[1] = duplicate[1] with { ClassJobId = SupplyClasses.Carpenter };
        var invalid = SupplyFixtures.Snapshot(duplicate);
        var character = CharacterFixtures.Standard();

        Assert.Empty(DecisionBuilder.Build(unloaded, character, new OwnedCount[SnapshotParser.EntryCount], Prices(), Defaults));
        Assert.Empty(DecisionBuilder.Build(invalid, character, new OwnedCount[SnapshotParser.EntryCount], Prices(), Defaults));
        Assert.Empty(DecisionBuilder.Build(RequestSnapshot.Unread(SupplyFixtures.ReadAtUtc), character, [], Prices(), Defaults));
    }

    [Fact]
    public void OwnedCountsMustLineUpWithTheSlots()
    {
        Assert.Throws<ArgumentException>(() => DecisionBuilder.Build(Day(), CharacterFixtures.Standard(), new OwnedCount[10], Prices(), Defaults));
    }

    private static RowDecision[] Build(DecisionSettings settings) => Build(settings, Prices());

    private static RowDecision[] Build(DecisionSettings settings, ItemPrice[] prices)
        => DecisionBuilder.Build(Day(), CharacterFixtures.Standard(), Owned(), prices, settings);

    private static RequestSnapshot Day()
    {
        var entries = SupplyFixtures.OpenDayCopy();
        entries[BlacksmithSlot] = entries[BlacksmithSlot] with { RemainingToHandIn = 0 };
        return SupplyFixtures.Snapshot(entries);
    }

    private static OwnedCount[] Owned()
    {
        var owned = new OwnedCount[SnapshotParser.EntryCount];
        owned[2] = new OwnedCount(1, 0);
        owned[4] = new OwnedCount(0, 1);
        owned[5] = new OwnedCount(1, 0);
        owned[BlacksmithSlot] = new OwnedCount(1, 0);
        owned[10] = new OwnedCount(5, 0);
        return owned;
    }

    private static ItemPrice[] Prices() =>
    [
        Item(43273, 1, Listed(1, 5_000), Listed(1, 8_000)),
        Item(43269, 1, Listed(1, 3_000), None),
        Item(43314, 1, Listed(1, 10_000), Listed(1, 20_000)),
        Item(44157, 3, PerUnit(3, 1_000), PerUnit(3, 3_000)),
        Item(5106, 10, PerUnit(10, 210), None),
        Item(5509, 20, None, None),
        Item(43263, 1, Listed(1, 1), Listed(1, 1)),
    ];

    private static (byte, RowCoverage, bool, ItemQuality, int, long, long, long, int, long, long, long) Summary(RowDecision row)
        => (row.Request.ClassJobId, row.Coverage, row.Included, row.ChosenQuality, row.BuyQuantity, row.UnitPrice, row.EstimatedTotal,
            row.MaxUnitPrice, row.ExcessAllowance, row.Experience, row.Seals, row.GilPerThousandExperience);

    private static bool[] Flags(RowDecision[] rows, Func<RowDecision, bool> flag)
    {
        var flags = new bool[rows.Length];
        for (var index = 0; index < rows.Length; index++)
        {
            flags[index] = flag(rows[index]);
        }

        return flags;
    }

    private static RowCoverage[] Coverages(RowDecision[] rows)
    {
        var coverages = new RowCoverage[rows.Length];
        for (var index = 0; index < rows.Length; index++)
        {
            coverages[index] = rows[index].Coverage;
        }

        return coverages;
    }
}
