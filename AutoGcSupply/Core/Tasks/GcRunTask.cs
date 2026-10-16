using AutoGcSupply.Core.HandIn;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Requests;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks.Model;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.Tasks;

internal sealed partial class GcRunTask(GcRunSession session, QuoteTracker quotes, TimersPasses passes, Func<bool> settling) : AutoCommon
{
    public const int SettleBoundMs = HandInDriver.CleanupBoundMs + SettleMarginMs;

    private const int PollMs = TimeUnits.MillisecondsPerSecond;
    private const int SettleMarginMs = 5 * TimeUnits.MillisecondsPerSecond;

    protected override async Task Execute()
    {
        try
        {
            await RunSteps();
        }
        catch (OperationCanceledException) when (CancelToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Run: faulted");
            session.Fail(Loc.T(L.Run.FailedFault));
        }
    }

    private async Task RunSteps()
    {
        if (!await AwaitEarlierHandIn())
        {
            return;
        }

        if (!await ReadRequests())
        {
            return;
        }

        if (!await BuyMissing())
        {
            return;
        }

        if (DayKey.For(DateTime.UtcNow) != session.ReadDay && !await ReadRequests())
        {
            return;
        }

        var plan = CountBags();
        if (plan.Targets.Length > 0 && !await HandIn(plan))
        {
            return;
        }

        Finish();
    }

    private async Task<bool> AwaitEarlierHandIn()
    {
        if (!settling())
        {
            return true;
        }

        session.Tracker.SetStatus(Loc.T(L.Run.StatusClosingList));
        Diag("Run: a stopped hand-in is still closing the Personnel Officer's windows; waiting for it before going on.");
        await WaitUntilTimed(() => !settling(), SettleBoundMs, "run-earlier-hand-in");
        return !CancelToken.IsCancellationRequested;
    }

    private async Task<bool> ReadRequests()
    {
        session.Tracker.SetStep(RunStep.Reading, Loc.T(L.Run.StatusReading));
        var memory = GcSupplyReader.Read();
        var currentToday = passes.IsCurrent(memory.ContentId, DayKey.For(DateTime.UtcNow));
        var loader = new TimersLoader(passes, RunPlanRules.NeedsTimersPass(currentToday, session.HandInStarted, memory));
        await RunCancellable(loader, TimersLoader.WatchdogMs, "run-reading");
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }

        if (loader.Outcome != TimersLoadOutcome.Loaded)
        {
            Warn($"Run: today's requests did not load ({loader.Outcome}); the run ends.");
            session.Fail(Loc.T(L.Run.FailedNotLoaded));
            return false;
        }

        var snapshot = loader.Snapshot;
        var authorization = session.Authorization;
        if (snapshot.ContentId != authorization.ContentId)
        {
            Warn("Run: another character is logged in than the one the run was started for; the run ends.");
            session.Fail(Loc.T(L.Run.FailedOtherCharacter));
            return false;
        }

        if (snapshot.Day != session.ReadDay)
        {
            Diag($"Run: the supply day is now {snapshot.Day} (the run started on {authorization.Day}); only requests that still match are kept and nothing new is bought.");
        }

        session.ReadDay = snapshot.Day;
        var dropped = RunPlanRules.DropChanged(session.Plan, session.Progress, snapshot);
        if (dropped > 0)
        {
            Diag($"Run: {dropped} row(s) left the run, handed in elsewhere or changed since Start.");
        }

        session.PublishRows();
        return true;
    }

    private void Finish()
    {
        session.Tracker.SetStep(RunStep.Finishing, Loc.T(L.Run.StatusFinishing));
        var verdict = RunOutcomeRule.Completion(session.Progress);
        session.Conclude(verdict.End);
    }
}
