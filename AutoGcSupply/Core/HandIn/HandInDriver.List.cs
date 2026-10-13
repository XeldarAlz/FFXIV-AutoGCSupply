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

    private static readonly SupplyListTab[] tabOrder = [SupplyListTab.Supply, SupplyListTab.Provisioning];

    private int projectedSeals;

    private async Task<Halt> WorkTabs()
    {
        projectedSeals = HandInReads.Seals(grandCompany);
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
            var unexpected = await HaltOnUnexpected(talkUnexpected: true);
            if (Interrupted(unexpected))
            {
                return unexpected;
            }

            if (!await WaitUntilTimed(() => SupplyListReader.ShowsTab(tab), ListLoadTimeoutMs, "hand-in-list-ready", ListPollFrames))
            {
                return CancelToken.IsCancellationRequested
                    ? Halt.None
                    : new Halt(HandInPauseReason.ListClosed, $"the supply list stopped showing the {tab} tab");
            }

            var list = SupplyListReader.Read();
            RefreshStocks(tab);
            var match = RowMatcher.FindNext(list, targets, rows, stocks);
            if (!match.Found)
            {
                SkipUnmatched(list, tab);
                return Halt.None;
            }

            if (dryRun)
            {
                RecordDryRun(match);
                continue;
            }

            var halt = await DeliverMatch(match);
            if (Interrupted(halt))
            {
                return halt;
            }
        }

        return Halt.None;
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

    private void RecordDryRun(in RowMatch match)
    {
        var target = targets[match.TargetIndex];
        var decision = SealCapRule.Evaluate(projectedSeals, target.SealReward, sealCap, sealCapPolicy);
        if (decision.Action == SealCapAction.Skip)
        {
            SetRow(match.TargetIndex, rows[match.TargetIndex] with { Status = HandInRowStatus.Skipped, SealCap = SealCapAction.Skip });
            Diag($"Dry run: would skip {HandInReads.Describe(target)} on row {match.RowIndex}, {decision.SealsOverCap} seals over the cap of {sealCap}");
            return;
        }

        projectedSeals = SealCapRule.SealsAfter(projectedSeals, target.SealReward, sealCap);
        var sealsLost = decision.Action == SealCapAction.DeliverWithLoss ? decision.SealsOverCap : 0;
        SetRow(match.TargetIndex, new HandInRowResult(HandInRowStatus.WouldDeliver, RowMatchStatus.Matched, decision.Action, 0, sealsLost));
        Diag($"Dry run: would deliver {HandInReads.Describe(target)} from row {match.RowIndex}{(sealsLost > 0 ? $", losing {sealsLost} seals to the cap" : string.Empty)}");
    }

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
}
