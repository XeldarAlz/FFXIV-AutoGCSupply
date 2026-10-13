using AutoGcSupply.Core.HandIn;
using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Inventory;
using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Tasks.Model;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.Tasks;

internal sealed partial class GcRunTask
{
    private const int CleanupPollFrames = 4;

    public bool HandingIn { get; private set; }

    private HandInPlan CountBags()
    {
        session.Tracker.SetStatus(Loc.T(L.Run.StatusCounting));
        var owned = new OwnedCount[session.Plan.Length];
        for (var index = 0; index < owned.Length; index++)
        {
            if (session.Plan[index].Included)
            {
                owned[index] = BagCounter.Count(session.Plan[index].Request.ItemId);
            }
        }

        var plan = HandInPlan.Build(session.Plan, session.Progress, owned, session.ShopperDryRun);
        session.PublishRows();
        Diag($"Run: the bags cover {plan.Targets.Length} row(s) to hand in.");
        return plan;
    }

    private async Task<bool> HandIn(HandInPlan plan)
    {
        var shopperDryRunNow = ShopperProbe.Read() switch
        {
            ShopperState.Missing => session.StartedDry,
            ShopperState.Available => AutoShopperApi.Instance.DryRun(whenUnknown: true),
            _ => true,
        };
        var dryRun = shopperDryRunNow || session.ShopperDryRun;
        session.DryRun = dryRun;
        session.Tracker.SetDryRun(dryRun);
        session.DidWork = true;
        session.HandInStarted = true;
        var driver = new HandInDriver(new HandInRequest(plan.Targets, session.Authorization.SealCapPolicy, dryRun));
        var seenRevision = -1;
        IFramework.OnUpdateDelegate mirror = _ =>
        {
            if (driver.Revision == seenRevision)
            {
                return;
            }

            seenRevision = driver.Revision;
            MirrorHandIn(driver, plan);
        };

        ShowHandInStep(driver, plan);
        HandingIn = true;
        var finished = await RunDriver(driver, mirror);
        if (!finished)
        {
            await AwaitCleanup(driver);
        }

        MirrorRows(driver, plan);
        HandingIn = false;
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }

        if (!finished)
        {
            Warn("Run: the hand-in ran past its watchdog and was cancelled.");
            RunPlanRules.ReleaseHandingIn(session.Progress);
            session.PublishRows();
            session.NeedPlayer(NeedsPlayerReason.HandInPaused, RunText.HandInTimeout());
            return false;
        }

        ShowHandInStep(driver, plan);
        var result = driver.Result;
        var verdict = RunOutcomeRule.AfterHandIn(result, session.Progress, dryRun);
        switch (verdict.End)
        {
            case RunEnd.NeedsPlayer:
                session.NeedPlayer(verdict.Reason, RunText.HandInNeeds(verdict.Reason, result));
                return false;
            case RunEnd.Stopped:
                session.Conclude(RunEnd.Stopped);
                return false;
            default:
                return true;
        }
    }

    private async Task<bool> RunDriver(HandInDriver driver, IFramework.OnUpdateDelegate mirror)
    {
        Svc.Framework.Update += mirror;
        try
        {
            return await RunCancellable(driver, HandInDriver.WatchdogMs, "run-hand-in");
        }
        catch (OperationCanceledException) when (CancelToken.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            Svc.Framework.Update -= mirror;
        }
    }

    private async Task AwaitCleanup(HandInDriver driver)
    {
        var deadline = Environment.TickCount64 + HandInDriver.CleanupBoundMs;
        while (!driver.Settled && Environment.TickCount64 < deadline)
        {
            await Svc.Framework.DelayTicks(CleanupPollFrames);
        }

        if (!driver.Settled)
        {
            Warn($"Run: the hand-in did not finish closing the Personnel Officer's windows within {HandInDriver.CleanupBoundMs / TimeUnits.MillisecondsPerSecond}s.");
        }
    }

    private void MirrorHandIn(HandInDriver driver, in HandInPlan plan)
    {
        MirrorRows(driver, plan);
        ShowHandInStep(driver, plan);
    }

    private void MirrorRows(HandInDriver driver, in HandInPlan plan)
    {
        if (HandInMirror.Apply(driver.Rows, plan.RowOfTarget, session.Progress) > 0)
        {
            session.PublishRows();
        }
    }

    private void ShowHandInStep(HandInDriver driver, in HandInPlan plan)
    {
        switch (driver.Step)
        {
            case HandInStep.Starting or HandInStep.Travelling:
                session.Tracker.SetStep(RunStep.Travelling, Loc.T(L.Run.StatusTravelling));
                break;
            case HandInStep.ApproachingOfficer:
                session.Tracker.SetStep(RunStep.Travelling, Loc.T(L.Run.StatusApproaching));
                break;
            case HandInStep.PickingRow or HandInStep.Confirming or HandInStep.Delivering or HandInStep.Verifying when driver.CurrentTargetIndex != HandInDriver.NoTarget:
                session.Tracker.SetStep(RunStep.HandingIn, HandingInText(driver, plan));
                break;
            case HandInStep.ClosingList or HandInStep.LeavingMenu or HandInStep.Finished:
                session.Tracker.SetStep(RunStep.HandingIn, Loc.T(L.Run.StatusClosingList));
                break;
            default:
                session.Tracker.SetStep(RunStep.HandingIn, Loc.T(L.Run.StatusOpeningList));
                break;
        }
    }

    private string HandingInText(HandInDriver driver, in HandInPlan plan)
    {
        var rows = driver.Rows;
        var settled = 0;
        for (var targetIndex = 0; targetIndex < rows.Length; targetIndex++)
        {
            if (rows[targetIndex].Status is not (HandInRowStatus.Pending or HandInRowStatus.Delivering))
            {
                settled++;
            }
        }

        var rowIndex = plan.RowOfTarget[driver.CurrentTargetIndex];
        return Loc.T(L.Run.StatusHandingIn, session.ItemName(rowIndex), settled + 1, rows.Length);
    }
}
