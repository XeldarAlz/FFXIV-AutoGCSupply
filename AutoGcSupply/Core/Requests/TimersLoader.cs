using AutoGcSupply.Core.Game.Ops;
using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Tasks;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.Requests;

public sealed class TimersLoader(TimersPasses passes, bool forcePass) : AutoCommon
{
    public const int MaxAttempts = 3;

    private const string TimersCommand = "/timers";
    private const string ContentsInfoAddon = "ContentsInfo";
    private const int CloseCallback = -1;
    private const int PlayerFreeWaitMs = 10_000;
    private const int PreCommandSettleMs = 500;
    private const int OpenWaitMs = 5_000;
    private const int DataWaitMs = 3_000;
    private const int RefreshSettleMs = 1_500;
    private const int CloseWaitMs = 3_000;
    private const int PassBudgetMs = PlayerFreeWaitMs + PreCommandSettleMs + OpenWaitMs + DataWaitMs + CloseWaitMs;

    public const int WatchdogMs = (MaxAttempts + 1) * PassBudgetMs;

    public RequestSnapshot Snapshot { get; private set; }

    public TimersLoadOutcome Outcome { get; private set; } = TimersLoadOutcome.Cancelled;

    protected override async Task Execute()
    {
        Status = "Reading today's requests";
        Snapshot = GcSupplyReader.Read();
        var passed = false;
        for (var attempt = 1; attempt <= MaxAttempts && ((forcePass && !passed) || !Snapshot.Loaded); attempt++)
        {
            if (!await WaitUntilTimed(PlayerFree, PlayerFreeWaitMs, $"timers-player-free#{attempt}"))
            {
                Finish(CancelToken.IsCancellationRequested ? TimersLoadOutcome.Cancelled : TimersLoadOutcome.PlayerBusy);
                return;
            }

            var shown = await PassThroughTimers(attempt, Snapshot.Loaded);
            if (CancelToken.IsCancellationRequested)
            {
                Finish(TimersLoadOutcome.Cancelled);
                return;
            }

            Snapshot = GcSupplyReader.Read();
            if (shown && Snapshot.Loaded)
            {
                passed = true;
                passes.Record(Snapshot.ContentId, Snapshot.Day);
            }
        }

        Finish(forcePass && !passed && Snapshot.Loaded ? TimersLoadOutcome.NotLoaded : OutcomeOf(Snapshot.LoadState));
    }

    private async Task<bool> PassThroughTimers(int attempt, bool dataPresent)
    {
        Status = "Loading today's requests from the Timers window";
        var openedHere = !NpcInteraction.IsAddonReady(ContentsInfoAddon);
        try
        {
            if (openedHere && !await OpenTimers(attempt))
            {
                return false;
            }

            if (dataPresent)
            {
                Diag("Timers loader: letting the Timers window refresh the requests before trusting them.");
                await DelayMs(RefreshSettleMs);
                return true;
            }

            await WaitUntilTimed(RequestsLoaded, DataWaitMs, $"timers-data#{attempt}");
            return true;
        }
        finally
        {
            if (openedHere)
            {
                await CloseTimers(attempt);
            }
        }
    }

    private async Task<bool> OpenTimers(int attempt)
    {
        await DelayMs(PreCommandSettleMs);
        if (CancelToken.IsCancellationRequested)
        {
            return false;
        }

        Diag($"Timers loader: opening the Timers window (attempt {attempt}/{MaxAttempts}).");
        Chat.ExecuteCommand(TimersCommand);
        if (await WaitUntilTimed(TimersReady, OpenWaitMs, $"timers-open#{attempt}"))
        {
            return true;
        }

        if (!CancelToken.IsCancellationRequested)
        {
            Warn($"Timers loader: the Timers window did not open within {OpenWaitMs / TimeUnits.MillisecondsPerSecond}s.");
        }

        return false;
    }

    private async Task CloseTimers(int attempt)
    {
        if (CancelToken.IsCancellationRequested)
        {
            NpcInteraction.FireCallback(ContentsInfoAddon, CloseCallback);
            return;
        }

        if (!await WaitUntilTimed(TimersClosedAfterNudge, CloseWaitMs, $"timers-close#{attempt}") && !CancelToken.IsCancellationRequested)
        {
            Warn("Timers loader: the Timers window did not close; it is left open.");
        }
    }

    private void Finish(TimersLoadOutcome outcome)
    {
        Outcome = outcome;
        GcSupplyReader.Log(Snapshot);
        switch (outcome)
        {
            case TimersLoadOutcome.Loaded:
                Diag($"Timers loader: requests loaded, {Snapshot.OpenCount} open.");
                break;
            case TimersLoadOutcome.Cancelled:
                Diag("Timers loader: cancelled.");
                break;
            default:
                Warn($"Timers loader: finished as {outcome}; the requests cannot be trusted yet.");
                break;
        }
    }

    private static TimersLoadOutcome OutcomeOf(SnapshotLoadState loadState) => loadState switch
    {
        SnapshotLoadState.Loaded => TimersLoadOutcome.Loaded,
        SnapshotLoadState.Invalid => TimersLoadOutcome.Invalid,
        _ => TimersLoadOutcome.NotLoaded,
    };

    private static bool TimersReady() => NpcInteraction.IsAddonReady(ContentsInfoAddon);

    private static bool TimersClosedAfterNudge()
    {
        if (!NpcInteraction.IsAddonReady(ContentsInfoAddon))
        {
            return true;
        }

        NpcInteraction.FireCallback(ContentsInfoAddon, CloseCallback);
        return false;
    }

    private static bool RequestsLoaded() => GcSupplyReader.Read().Loaded;

    public static bool PlayerFree()
        => Svc.Objects.LocalPlayer is not null
        && !Svc.Condition[ConditionFlag.InCombat]
        && !Svc.Condition[ConditionFlag.BoundByDuty]
        && !Svc.Condition[ConditionFlag.BoundByDuty56]
        && !Svc.Condition[ConditionFlag.BoundByDuty95]
        && !GenericHelpers.IsOccupied();
}
