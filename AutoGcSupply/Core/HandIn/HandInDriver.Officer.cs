using AutoGcSupply.Core.Game.Ops;
using AutoGcSupply.Core.HandIn.Model;
using AutoGcSupply.Core.Tasks;
using AutoGcSupply.Core.Travel;
using clib.Extensions;
using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal sealed partial class HandInDriver
{
    private const float ArriveWithinMeters = 3.5f;
    private const float OfficerStepToleranceMeters = 2.5f;
    private const int OfficerSearchMs = 10_000;
    private const int ApproachWatchdogMs = 25_000;
    private const int MaxApproachAttempts = 2;
    private const int MenuOpenTimeoutMs = 30_000;
    private const int MaxInteractions = 3;
    private const int ReinteractGapMs = 3_000;
    private const int MaxMenuSelections = 3;
    private const int MenuReselectMs = 2_000;
    private const int ListLoadTimeoutMs = 10_000;
    private const int ListPollFrames = 6;
    private const int LeaveMenuWaitMs = 3_000;
    private const int LeaveMenuCallback = -1;
    private const string SelectStringAddonName = "SelectString";

    private bool listOpened;

    private async Task<Halt> ReachOfficer()
    {
        var cityName = TerritoryNames.Of(officer.TerritoryId);
        EnterStep(HandInStep.Travelling, $"Travelling to the Personnel Officer in {cityName}");
        if (!OfficerWithinReach())
        {
            await HoldWhileTyping("the trip to the Personnel Officer");
            var arrived = await TravelTo(officer.TerritoryId, officer.ApproachPoint, ArriveWithinMeters);
            if (CancelToken.IsCancellationRequested)
            {
                return Halt.None;
            }

            if (!arrived && Svc.ClientState.TerritoryType != officer.TerritoryId)
            {
                return new Halt(HandInPauseReason.TravelFailed, $"could not reach {cityName} (now in territory {Svc.ClientState.TerritoryType})");
            }
        }

        if (Svc.Condition[ConditionFlag.Mounted] && !await SafeDismount("hand-in-dismount") && !CancelToken.IsCancellationRequested)
        {
            Warn("Hand-in: could not dismount near the Personnel Officer");
        }

        EnterStep(HandInStep.ApproachingOfficer, "Walking up to the Personnel Officer");
        if (!await WaitUntilTimed(() => NpcInteraction.FindNearest(officer.BaseId) is not null, OfficerSearchMs, "hand-in-officer-visible"))
        {
            return CancelToken.IsCancellationRequested
                ? Halt.None
                : new Halt(HandInPauseReason.OfficerNotFound, $"no Personnel Officer ({officer.BaseId}) in sight in {cityName}");
        }

        for (var attempt = 1; attempt <= MaxApproachAttempts && !OfficerWithinReach(); attempt++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return Halt.None;
            }

            await ApproachOfficer(attempt);
        }

        if (CancelToken.IsCancellationRequested || OfficerWithinReach())
        {
            return Halt.None;
        }

        return new Halt(HandInPauseReason.OfficerUnreachable, $"still {DistanceToOfficer():F1}m from the Personnel Officer in {cityName}");
    }

    private async Task ApproachOfficer(int attempt)
    {
        if (NpcInteraction.FindNearest(officer.BaseId) is not { } officerObject)
        {
            return;
        }

        await HoldWhileTyping("the walk to the Personnel Officer");
        var position = officerObject.Position;
        Diag($"Hand-in: walking up to the Personnel Officer, {DistanceToOfficer():F1}m away (attempt {attempt}/{MaxApproachAttempts})");
        var approach = new MoveOp(move => move.MoveInZone(position, MovementConfig.Default.WithTolerance(OfficerStepToleranceMeters), null));
        await RunCancellable(approach, ApproachWatchdogMs, "hand-in-approach", StuckDetector.MoveStallAbort("hand-in-approach"));
        if (approach.Fault is { } fault)
        {
            Diag($"Hand-in: the walk to the Personnel Officer faulted: {fault.Message}");
        }
    }

    private bool OfficerWithinReach()
        => Svc.Objects.LocalPlayer is not null && NpcInteraction.FindNearest(officer.BaseId) is { } officerObject && officerObject.IsInInteractRange();

    private float DistanceToOfficer()
        => Svc.Objects.LocalPlayer is { } player && NpcInteraction.FindNearest(officer.BaseId) is { } officerObject
            ? Vector3.Distance(player.Position, officerObject.Position)
            : float.MaxValue;

    private async Task<Halt> OpenSupplyList()
    {
        EnterStep(HandInStep.OpeningMenu, "Talking to the Personnel Officer");
        var optionText = OfficerMenuText.SupplyOption();
        if (optionText.Length == 0)
        {
            return new Halt(HandInPauseReason.MenuTextUnavailable, "the supply option text could not be read from the game data");
        }

        var deadline = Environment.TickCount64 + MenuOpenTimeoutMs;
        var interactions = 0;
        var selections = 0;
        var nextInteractAt = 0L;
        var nextSelectAt = 0L;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return Halt.None;
            }

            if (SupplyListReader.IsOpen())
            {
                listOpened = true;
                return await WaitForListLoaded();
            }

            var unexpected = await HaltOnUnexpected(talkUnexpected: false);
            if (unexpected.Stopped)
            {
                return unexpected;
            }

            var now = Environment.TickCount64;
            if (NpcInteraction.SelectStringOpen())
            {
                var entryIndex = FindMenuEntry(optionText);
                if (entryIndex == NpcInteraction.NoEntry)
                {
                    var shown = DescribeMenu();
                    await LeaveMenu();
                    return new Halt(HandInPauseReason.MenuOptionNotFound, $"'{optionText}' is not among the menu entries [{shown}]");
                }

                if (now >= nextSelectAt && selections < MaxMenuSelections && NpcInteraction.SelectEntry(entryIndex))
                {
                    selections++;
                    nextSelectAt = now + MenuReselectMs;
                    Diag($"Hand-in: chose '{optionText}' (entry {entryIndex}) from the officer's menu");
                }
            }
            else if (NpcInteraction.TalkOpen())
            {
                NpcInteraction.ProgressTalk();
            }
            else if (now >= nextInteractAt && interactions < MaxInteractions && NpcInteraction.PlayerReady() && NpcInteraction.FindNearest(officer.BaseId) is { } officerObject)
            {
                interactions++;
                nextInteractAt = now + ReinteractGapMs;
                Diag($"Hand-in: talking to the Personnel Officer (attempt {interactions}/{MaxInteractions})");
                NpcInteraction.Target(officerObject);
                NpcInteraction.Interact(officerObject);
            }

            await DelayMs(PollMs);
        }

        return new Halt(HandInPauseReason.ListDidNotOpen, $"the supply list did not open within {MenuOpenTimeoutMs / TimeUnits.MillisecondsPerSecond}s ({NpcInteraction.DescribeBlockers()})");
    }

    private async Task<Halt> WaitForListLoaded()
    {
        EnterStep(HandInStep.OpeningList, "Opening the supply list");
        if (await WaitUntilTimed(SupplyListReader.IsLoaded, ListLoadTimeoutMs, "hand-in-list-loaded", ListPollFrames))
        {
            return Halt.None;
        }

        return CancelToken.IsCancellationRequested
            ? Halt.None
            : new Halt(HandInPauseReason.ListDidNotOpen, "the supply list opened but never finished loading");
    }

    private async Task CloseListAndLeave()
    {
        EnterStep(HandInStep.ClosingList, "Closing the supply list");
        await CloseAddon(HandInClicks.RewardAddonName);
        if (SupplyListReader.IsOpen() && !await RepeatUntil(HandInClicks.CloseList, () => !SupplyListReader.IsOpen(), ClickWaitMs))
        {
            Warn("Hand-in: the supply list did not close through its own callback; closing the window");
            await CloseAddon(SupplyListReader.AddonName);
        }

        EnterStep(HandInStep.LeavingMenu, "Leaving the Personnel Officer");
        if (!listOpened)
        {
            await LeaveMenu();
            return;
        }

        var deadline = Environment.TickCount64 + LeaveMenuWaitMs;
        while (Environment.TickCount64 < deadline && !CancelToken.IsCancellationRequested)
        {
            if (NpcInteraction.SelectStringOpen())
            {
                await LeaveMenu();
                return;
            }

            if (NpcInteraction.TalkOpen())
            {
                NpcInteraction.ProgressTalk();
            }
            else if (!GenericHelpers.IsOccupied())
            {
                return;
            }

            await DelayMs(PollMs);
        }
    }

    private async Task LeaveMenu()
    {
        if (!NpcInteraction.SelectStringOpen())
        {
            return;
        }

        Diag("Hand-in: leaving the officer's menu");
        if (!await RepeatUntil(() => NpcInteraction.FireCallback(SelectStringAddonName, LeaveMenuCallback), () => !NpcInteraction.SelectStringOpen(), ClickWaitMs))
        {
            Warn("Hand-in: the officer's menu did not close");
        }
    }

    private static int FindMenuEntry(string optionText)
    {
        var entryCount = NpcInteraction.SelectStringEntryCount();
        for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (PromptText.Same(NpcInteraction.SelectStringEntryText(entryIndex), optionText))
            {
                return entryIndex;
            }
        }

        return NpcInteraction.NoEntry;
    }

    private static string DescribeMenu()
    {
        var builder = new StringBuilder();
        var entryCount = NpcInteraction.SelectStringEntryCount();
        for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            if (builder.Length > 0)
            {
                builder.Append(" | ");
            }

            builder.Append(NpcInteraction.SelectStringEntryText(entryIndex));
        }

        return builder.ToString();
    }
}
