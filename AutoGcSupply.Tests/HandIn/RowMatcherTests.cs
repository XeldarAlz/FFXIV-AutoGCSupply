using AutoGcSupply.Core.HandIn.Model;

namespace AutoGcSupply.Tests.HandIn;

public sealed class RowMatcherTests
{
    private const uint CarpenterItem = 2_001;
    private const uint BlacksmithItem = 2_002;
    private const uint CulinarianItem = 2_003;
    private const uint MinerItem = 2_004;
    private const byte Carpenter = 8;
    private const byte Blacksmith = 9;
    private const byte Culinarian = 15;
    private const byte Miner = 16;
    private const int Plenty = 99;

    private static readonly SupplyListRow carpenterRow = new(0, CarpenterItem, Carpenter, 1, false);
    private static readonly SupplyListRow blacksmithRow = new(0, BlacksmithItem, Blacksmith, 1, true);
    private static readonly SupplyListRow culinarianRow = new(0, CulinarianItem, Culinarian, 3, false);
    private static readonly SupplyListRow minerRow = new(0, MinerItem, Miner, 10, false);

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(2, 1, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 0, 1)]
    public void PicksTheRowByItemAndClassWhateverTheSortOrder(int carpenterPosition, int blacksmithPosition, int culinarianPosition)
    {
        var rows = new SupplyListRow[3];
        rows[carpenterPosition] = carpenterRow with { RowIndex = carpenterPosition };
        rows[blacksmithPosition] = blacksmithRow with { RowIndex = blacksmithPosition };
        rows[culinarianPosition] = culinarianRow with { RowIndex = culinarianPosition };
        var list = SupplyList(rows);

        Assert.Equal(carpenterPosition, Evaluate(list, Target(CarpenterItem, Carpenter, 1)).RowIndex);
        Assert.Equal(blacksmithPosition, Evaluate(list, Target(BlacksmithItem, Blacksmith, 1)).RowIndex);
        Assert.Equal(culinarianPosition, Evaluate(list, Target(CulinarianItem, Culinarian, 3)).RowIndex);
    }

    [Fact]
    public void UsesTheRowIndexTheListReportsAndNotThePositionInTheArray()
    {
        var list = SupplyList(carpenterRow with { RowIndex = 4 }, blacksmithRow with { RowIndex = 6 });

        var match = Evaluate(list, Target(BlacksmithItem, Blacksmith, 1));

        Assert.True(match.Found);
        Assert.Equal(6, match.RowIndex);
    }

    [Fact]
    public void RefusesARowThatIsNoLongerOpen()
    {
        var list = SupplyList([carpenterRow], [Agent(CarpenterItem, Plenty, turnInAvailable: false)]);

        Assert.Equal(RowMatchStatus.Closed, Evaluate(list, Target(CarpenterItem, Carpenter, 1)).Status);
    }

    [Fact]
    public void RefusesARowWhoseItemBelongsToAnotherClass()
    {
        var list = SupplyList(carpenterRow);

        Assert.Equal(RowMatchStatus.ClassMismatch, Evaluate(list, Target(CarpenterItem, Blacksmith, 1)).Status);
    }

    [Fact]
    public void ReportsAnItemTheListDoesNotShow()
    {
        var list = SupplyList(carpenterRow);

        Assert.Equal(RowMatchStatus.NotListed, Evaluate(list, Target(BlacksmithItem, Blacksmith, 1)).Status);
    }

    [Fact]
    public void RefusesTwoRowsForTheSameRequest()
    {
        var list = SupplyList(carpenterRow, carpenterRow with { RowIndex = 1 });

        Assert.Equal(RowMatchStatus.DuplicateRow, Evaluate(list, Target(CarpenterItem, Carpenter, 1)).Status);
    }

    [Fact]
    public void RefusesARowWhoseRequestedAmountChanged()
    {
        var list = SupplyList(culinarianRow);

        Assert.Equal(RowMatchStatus.RequestChanged, Evaluate(list, Target(CulinarianItem, Culinarian, 2)).Status);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    public void NeedsTheWholeRequestedAmountInTheBags(int normalInBags, bool expectedFound)
    {
        var list = SupplyList(culinarianRow);

        var match = Evaluate(list, Target(CulinarianItem, Culinarian, 3), new BagStock(normalInBags, 0));

        Assert.Equal(expectedFound, match.Found);
        Assert.Equal(expectedFound ? RowMatchStatus.Matched : RowMatchStatus.NotEnoughInBags, match.Status);
    }

    [Theory]
    [InlineData(true, 5, 0, false)]
    [InlineData(true, 0, 1, true)]
    [InlineData(false, 0, 3, false)]
    [InlineData(false, 1, 3, true)]
    public void CountsOnlyTheChosenQuality(bool high, int normalInBags, int highInBags, bool expectedFound)
    {
        var list = SupplyList(carpenterRow);
        var quality = high ? HandInQuality.High : HandInQuality.Normal;

        var match = Evaluate(list, Target(CarpenterItem, Carpenter, 1, quality), new BagStock(normalInBags, highInBags));

        Assert.Equal(expectedFound, match.Found);
    }

    [Fact]
    public void RefusesWhenTheGameCountsFewerThanTheBags()
    {
        var list = SupplyList([culinarianRow], [Agent(CulinarianItem, 2, turnInAvailable: true)]);

        Assert.Equal(RowMatchStatus.AgentDisagrees, Evaluate(list, Target(CulinarianItem, Culinarian, 3)).Status);
    }

    [Fact]
    public void RefusesWhenTheGameHasNoEntryForTheItem()
    {
        var list = SupplyList([carpenterRow], [Agent(BlacksmithItem, Plenty, turnInAvailable: true)]);

        Assert.Equal(RowMatchStatus.AgentMissing, Evaluate(list, Target(CarpenterItem, Carpenter, 1)).Status);
    }

    [Fact]
    public void LeavesAProvisioningTargetForTheProvisioningTab()
    {
        var supplyTab = SupplyList(carpenterRow);
        var provisioningTab = SupplyList(SupplyListTab.Provisioning, [minerRow], [Agent(MinerItem, Plenty, turnInAvailable: true)]);

        Assert.Equal(RowMatchStatus.OtherTab, Evaluate(supplyTab, Target(MinerItem, Miner, 10)).Status);
        Assert.True(Evaluate(provisioningTab, Target(MinerItem, Miner, 10)).Found);
    }

    [Fact]
    public void ReportsAListThatHasNotLoaded()
    {
        Assert.Equal(RowMatchStatus.ListNotLoaded, Evaluate(SupplyListState.NotLoaded, Target(CarpenterItem, Carpenter, 1)).Status);
    }

    [Theory]
    [InlineData(0u, 8, 1, false)]
    [InlineData(CarpenterItem, 8, 0, false)]
    [InlineData(CarpenterItem, 7, 1, false)]
    [InlineData(CarpenterItem, 19, 1, false)]
    [InlineData(MinerItem, 16, 10, true)]
    public void RefusesAMalformedTarget(uint itemId, int classJobId, int requested, bool provisioningHigh)
    {
        var quality = provisioningHigh ? HandInQuality.High : HandInQuality.Normal;
        var list = SupplyList(carpenterRow);

        Assert.Equal(RowMatchStatus.InvalidTarget, Evaluate(list, Target(itemId, (byte)classJobId, requested, quality)).Status);
    }

    [Fact]
    public void FindNextPicksTheFirstPendingTargetThatCanBeDelivered()
    {
        var list = SupplyList(carpenterRow, blacksmithRow with { RowIndex = 1 }, culinarianRow with { RowIndex = 2 });
        HandInTarget[] targets =
        [
            Target(CarpenterItem, Carpenter, 1),
            Target(BlacksmithItem, Blacksmith, 1),
            Target(CulinarianItem, Culinarian, 3),
        ];
        HandInRowStatus[] statuses = [HandInRowStatus.Delivered, HandInRowStatus.Pending, HandInRowStatus.Pending];
        BagStock[] stocks = [new(Plenty, 0), new(0, 0), new(Plenty, 0)];

        var match = RowMatcher.FindNext(list, targets, statuses, stocks);

        Assert.True(match.Found);
        Assert.Equal(2, match.TargetIndex);
        Assert.Equal(2, match.RowIndex);
    }

    [Fact]
    public void FindNextReportsWhyTheFirstPendingTargetCannotBeDelivered()
    {
        var list = SupplyList(carpenterRow, blacksmithRow with { RowIndex = 1 });
        HandInTarget[] targets = [Target(MinerItem, Miner, 10), Target(CarpenterItem, Carpenter, 1), Target(BlacksmithItem, Blacksmith, 1)];
        HandInRowStatus[] statuses = [HandInRowStatus.Pending, HandInRowStatus.Pending, HandInRowStatus.Pending];
        BagStock[] stocks = [new(Plenty, 0), new(0, 0), new(0, 0)];

        var match = RowMatcher.FindNext(list, targets, statuses, stocks);

        Assert.False(match.Found);
        Assert.Equal(1, match.TargetIndex);
        Assert.Equal(RowMatchStatus.NotEnoughInBags, match.Status);
    }

    [Fact]
    public void FindNextReportsNothingPendingOnceEveryTargetIsSettled()
    {
        var list = SupplyList(carpenterRow);
        HandInTarget[] targets = [Target(CarpenterItem, Carpenter, 1)];
        HandInRowStatus[] statuses = [HandInRowStatus.Delivered];
        BagStock[] stocks = [new(Plenty, 0)];

        var match = RowMatcher.FindNext(list, targets, statuses, stocks);

        Assert.Equal(RowMatchStatus.NothingPending, match.Status);
        Assert.Equal(RowMatch.NoTarget, match.TargetIndex);
    }

    [Fact]
    public void FindNextWaitsForTheListToLoad()
    {
        HandInTarget[] targets = [Target(MinerItem, Miner, 10)];
        HandInRowStatus[] statuses = [HandInRowStatus.Pending];
        BagStock[] stocks = [new(Plenty, 0)];

        Assert.Equal(RowMatchStatus.ListNotLoaded, RowMatcher.FindNext(SupplyListState.NotLoaded, targets, statuses, stocks).Status);
    }

    private static RowMatch Evaluate(in SupplyListState list, in HandInTarget target)
        => RowMatcher.Evaluate(list, target, new BagStock(Plenty, Plenty), 0);

    private static RowMatch Evaluate(in SupplyListState list, in HandInTarget target, BagStock stock)
        => RowMatcher.Evaluate(list, target, stock, 0);

    private static HandInTarget Target(uint itemId, byte classJobId, int requested, HandInQuality quality = HandInQuality.Normal)
        => new(itemId, classJobId, requested, quality, 100);

    private static SupplyAgentItem Agent(uint itemId, int possessed, bool turnInAvailable)
        => new(itemId, 0, possessed, 1, turnInAvailable);

    private static SupplyListState SupplyList(params SupplyListRow[] rows)
        => SupplyList(SupplyListTab.Supply, rows, OpenAgentItems(rows));

    private static SupplyListState SupplyList(SupplyListRow[] rows, SupplyAgentItem[] agentItems)
        => SupplyList(SupplyListTab.Supply, rows, agentItems);

    private static SupplyListState SupplyList(SupplyListTab tab, SupplyListRow[] rows, SupplyAgentItem[] agentItems)
        => new(true, tab, rows, agentItems);

    private static SupplyAgentItem[] OpenAgentItems(SupplyListRow[] rows)
    {
        var agentItems = new SupplyAgentItem[rows.Length];
        for (var rowPosition = 0; rowPosition < rows.Length; rowPosition++)
        {
            agentItems[rowPosition] = Agent(rows[rowPosition].ItemId, Plenty, turnInAvailable: true);
        }

        return agentItems;
    }
}
