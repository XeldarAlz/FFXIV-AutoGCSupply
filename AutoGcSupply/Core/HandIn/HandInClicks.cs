using AutoGcSupply.Core.HandIn.Model;
using ECommons;
using ECommons.Automation;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class HandInClicks
{
    public const string RewardAddonName = "GrandCompanySupplyReward";

    private const string SelectYesnoAddonName = "SelectYesno";
    private const int ClickThrottleMs = 600;
    private const int TabCallback = 0;
    private const int RowCallback = 1;
    private const int CloseCallback = -1;
    private const int RewardCancelCallback = 1;
    private const string TabThrottleKey = "AutoGcSupply.HandIn.Tab";
    private const string RowThrottleKey = "AutoGcSupply.HandIn.Row";
    private const string CloseListThrottleKey = "AutoGcSupply.HandIn.CloseList";
    private const string DeliverThrottleKey = "AutoGcSupply.HandIn.Deliver";
    private const string CancelRewardThrottleKey = "AutoGcSupply.HandIn.CancelReward";
    private const string PromptThrottleKey = "AutoGcSupply.HandIn.Prompt";

    public static bool RewardOpen() => AddonReady(RewardAddonName, out _);

    public static bool DeliverEnabled()
        => AddonReady(RewardAddonName, out var reward) && DeliverButtonEnabled(new AddonMaster.GrandCompanySupplyReward(reward));

    public static bool SelectTab(SupplyListTab tab)
    {
        if (!AddonReady(SupplyListReader.AddonName, out var list) || !EzThrottler.Throttle(TabThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        RunLog.Debug($"Hand-in: switching the supply list to the {tab} tab");
        Callback.Fire(list, true, TabCallback, (int)tab);
        return true;
    }

    public static bool ChooseRow(int rowIndex)
    {
        if (RewardOpen() || !AddonReady(SupplyListReader.AddonName, out var list) || !EzThrottler.Throttle(RowThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        RunLog.Debug($"Hand-in: choosing supply list row {rowIndex}");
        Callback.Fire(list, true, RowCallback, rowIndex, Callback.ZeroAtkValue);
        return true;
    }

    public static bool CloseList()
    {
        if (!AddonReady(SupplyListReader.AddonName, out var list) || !EzThrottler.Throttle(CloseListThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        RunLog.Debug("Hand-in: closing the supply list");
        Callback.Fire(list, true, CloseCallback);
        return true;
    }

    public static bool PressDeliver()
    {
        if (!AddonReady(RewardAddonName, out var reward) || !EzThrottler.Throttle(DeliverThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        var master = new AddonMaster.GrandCompanySupplyReward(reward);
        if (!DeliverButtonEnabled(master))
        {
            return false;
        }

        RunLog.Debug("Hand-in: pressing Deliver");
        master.Deliver();
        return true;
    }

    public static bool CancelReward()
    {
        if (!AddonReady(RewardAddonName, out var reward) || !EzThrottler.Throttle(CancelRewardThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        RunLog.Debug("Hand-in: pressing Cancel on the reward window");
        Callback.Fire(reward, true, RewardCancelCallback);
        return true;
    }

    public static bool AcceptPrompt()
    {
        if (!AddonReady(SelectYesnoAddonName, out var prompt) || !EzThrottler.Throttle(PromptThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        RunLog.Debug("Hand-in: answering Yes to the high-quality confirmation");
        new AddonMaster.SelectYesno(prompt) { RespectDisabledButtons = true }.Yes();
        return true;
    }

    private static bool DeliverButtonEnabled(AddonMaster.GrandCompanySupplyReward reward)
        => reward.DeliverButton != null && reward.IsEnabled;

    private static bool AddonReady(string addonName, out AtkUnitBase* addon)
        => GenericHelpers.TryGetAddonByName(addonName, out addon) && GenericHelpers.IsAddonReady(addon);
}
