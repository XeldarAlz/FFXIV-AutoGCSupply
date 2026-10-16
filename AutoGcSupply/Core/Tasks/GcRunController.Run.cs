using AutoGcSupply.Core.Ipc;
using AutoGcSupply.Core.Localization;
using AutoGcSupply.Core.Shopper;
using AutoGcSupply.Core.Stats;
using AutoGcSupply.Core.Tasks.Model;
using AutoGcSupply.Core.Value.Model;
using ECommons.DalamudServices;
using ClibServices = clib.Services.Svc;

namespace AutoGcSupply.Core.Tasks;

internal sealed partial class GcRunController
{
    private GcRunSession? session;
    private GcRunTask? currentTask;
    private GcRunTask? settlingTask;
    private long settlingUntilMs;

    public bool Settling => settlingTask is not null && Environment.TickCount64 < settlingUntilMs;

    public StartBlock Start()
    {
        if (session is not null)
        {
            RunLog.Info("Start ignored: a run is already running.");
            return StartBlock.Running;
        }

        viewedAtMs = Environment.TickCount64;
        ReadInputs();
        Rebuild();
        var model = today;
        if (model.StartBlock != StartBlock.None)
        {
            RunLog.Info($"Start refused: {model.StartBlock}.");
            return model.StartBlock;
        }

        var decisions = new RowDecision[model.Rows.Length];
        for (var index = 0; index < decisions.Length; index++)
        {
            decisions[index] = model.Rows[index].Decision;
        }

        var authorization = RunAuthorization.Freeze(
            Guid.NewGuid(),
            character,
            snapshot.Day,
            DateTime.UtcNow,
            model.SpendCap,
            settings.PriceTolerancePercent,
            configuration.SealCapPolicy,
            decisions);
        var active = new GcRunSession(authorization, tracker, labels);
        session = active;
        PauseReason = PauseReason.None;
        tracker.Begin(new RunRow[authorization.Rows.Length], Loc.T(L.Run.StatusReading));
        RunPlanRules.NoteUnpriced(active.Plan, active.Progress, model.Shopper);
        active.PublishRows();
        todayDirty = true;
        RunLog.Info(
            $"Run {authorization.RunId} authorized for day {authorization.Day}: {authorization.Rows.Length} row(s), " +
            $"{model.Totals.ToBuy} to buy for about {model.Totals.EstimatedSpend} gil under a cap of {authorization.RunCap} gil, " +
            $"tolerance {authorization.TolerancePercent}%, seal cap policy {authorization.SealCapPolicy}.");
        Launch(active);
        return StartBlock.None;
    }

    public void Stop()
    {
        if (session is not { } active)
        {
            return;
        }

        if (active.Buying)
        {
            if (!active.CanStopShopper)
            {
                return;
            }

            active.RequestStop(Environment.TickCount64);
            var accepted = AutoShopperApi.Instance.StopRun(active.ShopperRunId);
            tracker.SetStatus(Loc.T(L.Run.StatusStopping));
            RunLog.Info(accepted
                ? "Stop requested during buying; Auto Shopper stops its run and this run ends once it settles."
                : "Stop requested during buying; Auto Shopper did not take the stop, so this run ends once its run settles.");
            return;
        }

        if (active.End != RunEnd.None)
        {
            return;
        }

        StopTask();
        if (Settling)
        {
            StopAfterHandIn(active);
            return;
        }

        EndNow(active, "stopped by the player");
    }

    public void StopForAlert() => Stop();

    public void Interrupt()
    {
        if (session is not { } active)
        {
            return;
        }

        if (active.CanStopShopper)
        {
            AutoShopperApi.Instance.StopRun(active.ShopperRunId);
        }

        EndNow(active, "logged out or unloading");
    }

    public void Pause(PauseReason reason)
    {
        if (reason == PauseReason.None || session is not { End: RunEnd.None } active)
        {
            return;
        }

        if (Paused)
        {
            if (reason == PauseReason.Manual && PauseReason == PauseReason.InContent)
            {
                PauseReason = PauseReason.Manual;
                RunLog.Info("Auto-pause promoted to a manual pause; leaving content will no longer resume.");
            }

            return;
        }

        if (active.Buying)
        {
            if (reason == PauseReason.Manual)
            {
                RunLog.Info("Pause ignored: Auto Shopper is buying and owns the pause until its run ends.");
            }

            return;
        }

        PauseReason = reason;
        StopTask();
        tracker.SetStatus(reason == PauseReason.InContent ? Loc.T(L.Run.StatusPausedInContent) : Loc.T(L.Run.PhasePaused));
        RunLog.Info($"Run paused ({reason}); Resume reads the requests again and carries on.");
    }

    public void Resume()
    {
        if (!Paused)
        {
            return;
        }

        PauseReason = PauseReason.None;
        if (session is not { } active)
        {
            return;
        }

        RunLog.Info("Resuming the run from a fresh read of the requests.");
        Launch(active);
    }

    public void TogglePause()
    {
        if (Paused)
        {
            Resume();
            return;
        }

        Pause(PauseReason.Manual);
    }

    private void Launch(GcRunSession active)
    {
        try
        {
            var task = new GcRunTask(active, quotes, timersPasses, () => Settling);
            currentTask = task;
            ClibServices.Automation.Start(task, OnCompleted: () => _ = Svc.Framework.RunOnFrameworkThread(() => HandOff(task)));
        }
        catch (Exception exception)
        {
            currentTask = null;
            RunLog.Error(exception, "The run task could not be started");
            Svc.Chat.PrintError($"{GcSupplyConstants.LogPrefix} {Loc.T(L.Run.ChatTaskFailed)}");
            active.Fail(Loc.T(L.Run.FailedFault));
            Settle(active);
        }
    }

    private void HandOff(GcRunTask task)
    {
        if (ReferenceEquals(settlingTask, task))
        {
            settlingTask = null;
            RunLog.Info("The stopped hand-in finished closing the Personnel Officer's windows.");
            if (currentTask is null && session is { End: not RunEnd.None } stopped)
            {
                Settle(stopped);
            }

            return;
        }

        if (!ReferenceEquals(currentTask, task))
        {
            RunLog.Debug("A run task that was paused or stopped finished unwinding.");
            return;
        }

        currentTask = null;
        if (session is not { } active)
        {
            return;
        }

        if (active.End == RunEnd.None)
        {
            RunLog.Warning("The run task ended without an outcome; the run is marked failed.");
            active.Fail(Loc.T(L.Run.FailedFault));
        }

        Settle(active);
    }

    private void EndNow(GcRunSession active, string reason)
    {
        StopTask();
        if (active.End == RunEnd.None)
        {
            active.Conclude(RunEnd.Stopped);
        }

        RunLog.Info($"Run stopped: {reason}.");
        Settle(active);
    }

    private void StopAfterHandIn(GcRunSession active)
    {
        active.Conclude(RunEnd.Stopped);
        active.RequestStop(Environment.TickCount64);
        PauseReason = PauseReason.None;
        tracker.SetStatus(Loc.T(L.Run.StatusClosingList));
        RunLog.Info("Run stopped by the player; it ends once the stopped hand-in has closed the Personnel Officer's windows and reported its rows.");
    }

    private void StopTask()
    {
        if (currentTask is not { } task)
        {
            return;
        }

        currentTask = null;
        ClibServices.Automation.Stop();
        NavmeshIPC.Instance.Stop();
        if (!task.HandingIn)
        {
            return;
        }

        settlingTask = task;
        settlingUntilMs = Environment.TickCount64 + GcRunTask.SettleBoundMs;
        RunLog.Info("The stopped hand-in is closing the Personnel Officer's windows; nothing new starts until it is done.");
    }

    private void Settle(GcRunSession active)
    {
        if (!ReferenceEquals(session, active))
        {
            return;
        }

        session = null;
        PauseReason = PauseReason.None;
        RunPlanRules.ReleaseHandingIn(active.Progress);
        active.PublishRows();
        var authorization = active.Authorization;
        var authorized = authorization.Rows.AsSpan();
        var summary = RunOutcomeRule.Summarize(authorized, active.Progress, active.GilSpent, DateTime.UtcNow - authorization.StartedAtUtc);
        tracker.Finish(active.End, summary, RunText.Ended(active.End, summary, active.Failure));
        Record(active, summary);
        todayDirty = true;
        lastPollMs = 0;
        RunLog.Info(
            $"Run {authorization.RunId} ended {active.End}: {summary.Delivered}/{summary.Planned} delivered, " +
            $"{summary.Experience} XP, {summary.Seals} seals, {summary.SealsLost} lost to the cap, {summary.GilSpent} gil spent in {summary.Duration:hh\\:mm\\:ss}.");
    }

    private void Record(GcRunSession active, in RunSummary summary)
    {
        if (!active.DidWork && active.End == RunEnd.Stopped)
        {
            RunLog.Info("The run was stopped before it bought or handed in anything; nothing is recorded.");
            return;
        }

        var authorization = active.Authorization;
        history.Append(new RunRecord(
            authorization.RunId,
            authorization.StartedAtUtc,
            DateTime.UtcNow,
            active.End,
            authorization.WorldId,
            summary.Delivered,
            summary.Planned,
            summary.Experience,
            summary.Seals,
            summary.SealsLost,
            summary.GilSpent));
    }
}
