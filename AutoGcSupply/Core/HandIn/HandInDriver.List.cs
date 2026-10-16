using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory;
using AutoGcSupply.Core.Inventory.Model;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal sealed partial class HandInDriver
{
    private const int TabSwitchTimeoutMs = 6_000;
    private const int MaxTabClicks = 3;
    private const int TabReclickMs = 2_000;
    private const int PostDeliverySettleMs = 5_000;

    private static readonly SupplyListTab[] tabOrder = [SupplyListTab.Supply, SupplyListTab.Provisioning];

    private long listSettlesUntil;

    private async Task<Halt> WorkTabs()
    {
        for (var tabIndex = 0; tabIndex < tabOrder.Length; tabIndex++)
        {
            var tab = tabOrder[tabIndex];
            if (!AnyPendingOn(tab))
            {
                continue;
            }

            var halt = await ShowTab(tab);
            if (Interrupted(halt))
            {
                return halt;
            }

            halt = await WorkTab(tab);
            if (Interrupted(halt))
            {
                return halt;
            }
        }

        return Halt.None;
    }

    private async Task<Halt> ShowTab(SupplyListTab tab)
    {
        EnterStep(HandInStep.SelectingTab, $"Opening the {tab} tab");
        var deadline = Environment.TickCount64 + TabSwitchTimeoutMs;
        var clicks = 0;
        var nextClickAt = 0L;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return Halt.None;
            }

            var unexpected = await HaltOnUnexpected(talkUnexpected: true);
            if (unexpected.Stopped)
            {
                return unexpected;
            }

            if (!SupplyListReader.IsOpen())
            {
                return new Halt(HandInPauseReason.ListClosed, $"the supply list closed while opening the {tab} tab");
            }

            if (SupplyListReader.ShowsTab(tab))
            {
                Diag($"Hand-in: supply list on the {SupplyListReader.DescribeLayout(SupplyListReader.Read())}");
                return Halt.None;
            }

            var now = Environment.TickCount64;
            if (now >= nextClickAt && clicks < MaxTabClicks && HandInClicks.SelectTab(tab))
            {
                clicks++;
                nextClickAt = now + TabReclickMs;
            }

            await DelayMs(PollMs);
        }

        return new Halt(HandInPauseReason.TabDidNotSwitch, $"the supply list did not show the {tab} tab within {TabSwitchTimeoutMs / TimeUnits.MillisecondsPerSecond}s");
    }

    private async Task<Halt> WorkTab(SupplyListTab tab)
    {
        for (var pass = 0; pass <= targets.Length; pass++)
        {
            var next = await NextMatch(tab);
            if (Interrupted(next.Halt))
            {
                return next.Halt;
            }

            if (!next.Match.Found)
            {
                SkipUnmatched(next.List, tab);
                return Halt.None;
            }

            var halt = await DeliverMatch(next.Match);
            if (Interrupted(halt))
            {
                return halt;
            }
        }

        return Halt.None;
    }

    private async Task<TabPick> NextMatch(SupplyListTab tab)
    {
        var loadDeadline = Environment.TickCount64 + ListLoadTimeoutMs;
        var reportedMiss = RowMatchStatus.Matched;
        while (!CancelToken.IsCancellationRequested)
        {
            var unexpected = await HaltOnUnexpected(talkUnexpected: true);
            if (!Interrupted(unexpected))
            {
                unexpected = await CloseStaleRewardWindow();
            }

            if (Interrupted(unexpected))
            {
                return TabPick.Stopped(unexpected);
            }

            if (!await WaitUntilTimed(() => SupplyListReader.ShowsTab(tab), ListLoadTimeoutMs, "hand-in-list-ready", ListPollFrames))
            {
                return TabPick.Stopped(CancelToken.IsCancellationRequested
                    ? Halt.None
                    : new Halt(HandInPauseReason.ListClosed, $"the supply list stopped showing the {tab} tab"));
            }

            var list = SupplyListReader.Read();
            RefreshStocks(tab);
            var match = RowMatcher.FindNext(list, targets, rows, stocks);
            if (match.Found || match.Status == RowMatchStatus.NothingPending)
            {
                return new TabPick(Halt.None, list, match);
            }

            var now = Environment.TickCount64;
            switch (RowMissRule.Decide(match.Status, now < loadDeadline, now < listSettlesUntil))
            {
                case RowMissAction.Skip:
                    return new TabPick(Halt.None, list, match);
                case RowMissAction.Halt:
                    return TabPick.Stopped(new Halt(HandInPauseReason.ListClosed, $"the supply list did not finish loading the {tab} tab within {ListLoadTimeoutMs / TimeUnits.MillisecondsPerSecond}s"));
            }

            if (match.Status != reportedMiss)
            {
                reportedMiss = match.Status;
                Diag($"Hand-in: waiting for the supply list to settle before deciding on {DescribeTarget(match.TargetIndex)} ({match.Status})");
            }

            await DelayMs(PollMs);
        }

        return TabPick.Stopped(Halt.None);
    }

    private void RefreshStocks(SupplyListTab tab)
    {
        for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            if (rows[targetIndex].Status == HandInRowStatus.Pending && targets[targetIndex].Tab == tab)
            {
                stocks[targetIndex] = BagCounter.Count(targets[targetIndex].ItemId);
            }
        }
    }

    private void SkipUnmatched(in SupplyListState list, SupplyListTab tab)
    {
        for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            if (rows[targetIndex].Status != HandInRowStatus.Pending || targets[targetIndex].Tab != tab)
            {
                continue;
            }

            var miss = RowMatcher.Evaluate(list, targets[targetIndex], stocks[targetIndex], targetIndex);
            SetRow(targetIndex, rows[targetIndex] with { Status = HandInRowStatus.Skipped, Match = miss.Status });
            Warn($"Hand-in: skipping {HandInReads.Describe(targets[targetIndex])}: {DescribeMiss(miss.Status, stocks[targetIndex])}");
        }
    }

    private string DescribeTarget(int targetIndex)
        => targetIndex == NoTarget ? "the list" : HandInReads.Describe(targets[targetIndex]);

    private static string DescribeMiss(RowMatchStatus status, OwnedCount stock) => status switch
    {
        RowMatchStatus.NotListed => "the supply list does not show it",
        RowMatchStatus.ClassMismatch => "the supply list shows the item for another class",
        RowMatchStatus.DuplicateRow => "the supply list shows it more than once",
        RowMatchStatus.RequestChanged => "the requested amount differs from the authorized one",
        RowMatchStatus.AgentMissing => "the game has no supply entry for it",
        RowMatchStatus.Closed => "the game shows it as already handed in",
        RowMatchStatus.NotEnoughInBags => $"the bags hold {stock.Normal} NQ and {stock.High} HQ",
        RowMatchStatus.AgentDisagrees => "the game counts fewer than the bags hold",
        _ => status.ToString(),
    };

    private readonly record struct TabPick(Halt Halt, SupplyListState List, RowMatch Match)
    {
        public static TabPick Stopped(in Halt halt) => new(halt, SupplyListState.NotLoaded, RowMatch.Miss(RowMatchStatus.NothingPending, NoTarget));
    }
}
