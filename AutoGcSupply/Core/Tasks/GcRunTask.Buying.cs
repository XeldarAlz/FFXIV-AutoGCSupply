using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Tasks.Model;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.Tasks;

internal sealed partial class GcRunTask
{
    private const int MaxUnreadableReplies = 30;
    private const long StopWaitMs = 90_000;

    private ShopperRunView lastView = ShopperRunView.Failed(ShopperReply.Unavailable);

    private async Task<bool> BuyMissing()
    {
        if (session.ShopperEnded || !RunPlanRules.AnyPurchase(session.Plan))
        {
            return true;
        }

        if (!DayAllowsPurchases())
        {
            return true;
        }

        var shopper = ShopperProbe.Read();
        if (shopper != ShopperState.Available)
        {
            Unbought(UnboughtNote(shopper), $"Auto Shopper is {shopper}");
            return true;
        }

        if (!await PriceAgain())
        {
            return false;
        }

        if (!RunPlanRules.AnyPurchase(session.Plan) || !DayAllowsPurchases())
        {
            return true;
        }

        return await Shop();
    }

    private bool DayAllowsPurchases()
    {
        if (session.Authorization.AllowsPurchases(session.ReadDay, DateTime.UtcNow))
        {
            return true;
        }

        Unbought(RowNote.DayChanged, "a new supply day began after Start");
        return false;
    }

    private static RowNote UnboughtNote(ShopperState shopper) => shopper switch
    {
        ShopperState.Missing => RowNote.ShopperMissing,
        ShopperState.Newer => RowNote.ShopperNewer,
        _ => RowNote.ShopperOutdated,
    };

    private void Unbought(RowNote note, string reason)
    {
        var marked = RunPlanRules.MarkUnbought(session.Plan, session.Progress, note);
        Diag($"Run: buying skipped ({reason}); {marked} row(s) are not bought and the run hands in what the bags cover.");
        session.PublishRows();
    }

    private async Task<bool> PriceAgain()
    {
        var needed = QuoteLines.ForPurchases(session.Plan);
        if (quotes.Book.IsFresh(needed, DateTime.UtcNow))
        {
            Diag($"Run: reusing the quote from {quotes.Book.PricedAtUtc:HH:mm:ss} UTC.");
            return true;
        }

        session.Tracker.SetStep(RunStep.Pricing, Loc.T(L.Run.StatusPricing));
        Diag($"Run: pricing {needed.Length} item(s) again before buying.");
        quotes.Start(needed);
        while (quotes.Book.Status == QuoteStatus.Pending)
        {
            await DelayMs(PollMs);
            if (CancelToken.IsCancellationRequested)
            {
                return false;
            }

            quotes.Poll();
        }

        if (quotes.Book.Status != QuoteStatus.Ready)
        {
            Warn("Run: the new quote did not come back; buying with the caps authorized at Start.");
            return true;
        }

        var changed = RunPlanRules.ApplyFreshPrices(session.Plan, session.Progress, quotes.Book.Prices, session.Authorization.TolerancePercent);
        if (changed > 0)
        {
            Diag($"Run: the new quote lowered or closed {changed} line(s).");
            session.PublishRows();
        }

        return true;
    }

    private async Task<bool> Shop()
    {
        var authorization = session.Authorization;
        var lines = ShopperLineBuilder.Build(session.Plan);
        session.Tracker.SetStep(RunStep.Buying, Loc.T(L.Run.StatusBuying));
        session.Buying = true;
        var acceptance = AutoShopperApi.Instance.StartRun(authorization.RunId.ToString(), authorization.ShopperLabel, authorization.RunCap, returnHome: true, lines);
        if (!acceptance.Accepted)
        {
            session.Buying = false;
            var reason = acceptance.Reply == ShopperReply.Valid ? ShopperSettlementRule.ReasonForRefusal(acceptance.Refusal) : NeedsPlayerReason.ShopperRefused;
            Warn($"Run: Auto Shopper did not start the run ({acceptance.Reply}, {acceptance.RefusalName}): {acceptance.Message}");
            session.NeedPlayer(reason, RunText.Refusal(reason, acceptance));
            return false;
        }

        session.ShopperRunId = acceptance.Id;
        session.DidWork = true;
        Diag($"Run: Auto Shopper run {acceptance.Id} started with {lines.Length} line(s) and a cap of {authorization.RunCap} gil.");
        var settlement = await AwaitShopper(acceptance.Id);
        session.Buying = false;
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }

        session.ShopperEnded = true;
        Diag($"Run: Auto Shopper settled as {settlement.Kind} {settlement.Reason}; {session.GilSpent} gil spent.");
        switch (settlement.Kind)
        {
            case ShopperSettlementKind.Stopped:
                session.Conclude(RunEnd.Stopped);
                return false;
            case ShopperSettlementKind.NeedsPlayer:
                session.NeedPlayer(settlement.Reason, RunText.ShopperNeeds(settlement.Reason, lastView));
                return false;
            default:
                return true;
        }
    }

    private async Task<ShopperSettlement> AwaitShopper(string runId)
    {
        var api = AutoShopperApi.Instance;
        var unreadable = 0;
        var watch = new ShopperWatch(Environment.TickCount64);
        while (true)
        {
            var view = api.RunStatus(runId);
            var nowMs = Environment.TickCount64;
            var settlement = ShopperSettlementRule.Classify(view, session.StopRequested);
            if (settlement.Kind == ShopperSettlementKind.Unreadable)
            {
                unreadable++;
                if (unreadable >= MaxUnreadableReplies)
                {
                    return GiveUp(runId, $"Auto Shopper gave no usable status for run {runId} {MaxUnreadableReplies} times in a row ({view.Reply}, known {view.Known})");
                }
            }
            else
            {
                unreadable = 0;
                watch.Observe(view, nowMs);
                Mirror(view);
                if (settlement.Kind != ShopperSettlementKind.Running)
                {
                    return settlement;
                }
            }

            if (session.StopRequested && nowMs - session.StopRequestedAtMs >= StopWaitMs)
            {
                Warn($"Run: Auto Shopper did not settle within {StopWaitMs / TimeUnits.MillisecondsPerSecond}s of the stop; the run ends as stopped.");
                return ShopperSettlement.Stopped;
            }

            if (watch.Stalled(nowMs))
            {
                return GiveUp(runId, $"Auto Shopper run {runId} showed no change for {ShopperWatch.StallLimitMs / TimeUnits.MillisecondsPerMinute} minutes while not paused");
            }

            if (watch.Overdue(nowMs))
            {
                return GiveUp(runId, $"Auto Shopper run {runId} did not end within {ShopperWatch.TotalLimitMs / TimeUnits.MillisecondsPerMinute} minutes");
            }

            await DelayMs(PollMs);
            if (CancelToken.IsCancellationRequested)
            {
                return ShopperSettlement.Stopped;
            }
        }
    }

    private ShopperSettlement GiveUp(string runId, string reason)
    {
        var stopped = AutoShopperApi.Instance.StopRun(runId);
        Warn($"Run: {reason}; {(stopped ? "its run was asked to stop" : "it did not take the stop")} and the run needs you.");
        return ShopperSettlement.NeedsPlayer(NeedsPlayerReason.ShopperInterrupted);
    }

    private void Mirror(in ShopperRunView view)
    {
        lastView = view;
        session.GilSpent = view.ConfirmedSpend;
        session.DryRun = view.DryRun;
        session.ShopperDryRun = view.DryRun;
        session.Tracker.SetDryRun(view.DryRun);
        session.Tracker.SetShopper(view.Status, view.ConfirmedSpend, view.Paused);
        if (!session.StopRequested)
        {
            session.Tracker.SetStatus(view.Paused ? RunText.ShopperPaused(view.PauseReason) : Loc.T(L.Run.StatusBuying));
        }

        if (BuyProgress.Apply(session.Plan, session.Progress, view) > 0)
        {
            session.PublishRows();
        }
    }
}
