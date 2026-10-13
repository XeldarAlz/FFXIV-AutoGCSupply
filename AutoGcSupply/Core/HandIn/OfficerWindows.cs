using AutoGcSupply.Core.Game.Ops;
using ECommons;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoGcSupply.Core.HandIn;

internal static class OfficerWindows
{
    private const int ClickWaitMs = 3_000;
    private const int LeaveMenuWaitMs = 3_000;
    private const int LeaveMenuCallback = -1;
    private const int PollMs = 100;
    private const int PollFrames = 2;
    private const string SelectStringAddonName = "SelectString";

    public static async Task DeclinePrompt(Func<bool> stopped)
    {
        RunLog.Warning($"Hand-in: answering No to '{NpcInteraction.SelectYesnoText()}'");
        if (!await RepeatUntil(NpcInteraction.AnswerNo, PromptClosed, ClickWaitMs, stopped))
        {
            RunLog.Warning("Hand-in: the prompt did not close after No");
        }
    }

    public static async Task CloseAddon(string addonName, Func<bool> stopped)
    {
        if (!NpcInteraction.IsAddonReady(addonName))
        {
            return;
        }

        RunLog.Info($"Hand-in: closing {addonName}");
        if (!await RepeatUntil(() => NpcInteraction.Close(addonName), () => !NpcInteraction.IsAddonReady(addonName), ClickWaitMs, stopped))
        {
            RunLog.Warning($"Hand-in: {addonName} did not close");
        }
    }

    public static async Task<bool> CloseReward(Func<bool> stopped)
    {
        if (!HandInClicks.RewardOpen())
        {
            return true;
        }

        RunLog.Info("Hand-in: cancelling the reward window");
        if (await RepeatUntil(HandInClicks.CancelReward, RewardClosed, ClickWaitMs, stopped))
        {
            return true;
        }

        RunLog.Warning("Hand-in: the reward window did not close through Cancel; closing the window");
        await CloseAddon(HandInClicks.RewardAddonName, stopped);
        return !stopped() && RewardClosed();
    }

    public static async Task CloseList(Func<bool> stopped)
    {
        if (NpcInteraction.SelectYesnoOpen())
        {
            await DeclinePrompt(stopped);
        }

        await CloseReward(stopped);
        if (stopped() || !SupplyListReader.IsOpen() || await RepeatUntil(HandInClicks.CloseList, ListClosed, ClickWaitMs, stopped))
        {
            return;
        }

        RunLog.Warning("Hand-in: the supply list did not close through its own callback; closing the window");
        await CloseAddon(SupplyListReader.AddonName, stopped);
    }

    public static async Task LeaveOfficer(bool listOpened, Func<bool> stopped)
    {
        if (!listOpened)
        {
            await LeaveMenu(stopped);
            return;
        }

        var deadline = Environment.TickCount64 + LeaveMenuWaitMs;
        while (Environment.TickCount64 < deadline && !stopped())
        {
            if (NpcInteraction.SelectStringOpen())
            {
                await LeaveMenu(stopped);
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

            await Delay(PollMs, stopped);
        }
    }

    public static async Task LeaveMenu(Func<bool> stopped)
    {
        if (stopped() || !NpcInteraction.SelectStringOpen())
        {
            return;
        }

        RunLog.Info("Hand-in: leaving the officer's menu");
        if (!await RepeatUntil(FireLeaveMenu, MenuClosed, ClickWaitMs, stopped))
        {
            RunLog.Warning("Hand-in: the officer's menu did not close");
        }
    }

    private static async Task<bool> RepeatUntil(Func<bool> click, Func<bool> done, int timeoutMs, Func<bool> stopped)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline && !stopped())
        {
            if (done())
            {
                return true;
            }

            click();
            await Delay(PollMs, stopped);
        }

        return !stopped() && done();
    }

    private static async Task Delay(int milliseconds, Func<bool> stopped)
    {
        var deadline = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < deadline && !stopped())
        {
            await Svc.Framework.DelayTicks(PollFrames);
        }
    }

    private static bool PromptClosed() => !NpcInteraction.SelectYesnoOpen();

    private static bool RewardClosed() => !HandInClicks.RewardOpen();

    private static bool ListClosed() => !SupplyListReader.IsOpen();

    private static bool MenuClosed() => !NpcInteraction.SelectStringOpen();

    private static bool FireLeaveMenu() => NpcInteraction.FireCallback(SelectStringAddonName, LeaveMenuCallback);
}
