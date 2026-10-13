using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class UnexpectedAddons
{
    public const string TalkAddonName = "Talk";

    private const string DutyPopAddonName = "ContentsFinderConfirm";

    private static readonly string[] alwaysUnexpected = ["Request", "ContextIconMenu", "SelectIconString", "InputNumeric", "SelectOk"];

    public static bool TryFind(bool talkUnexpected, out string addonName, bool requestExpected = false)
    {
        for (var addonIndex = 0; addonIndex < alwaysUnexpected.Length; addonIndex++)
        {
            if (requestExpected && alwaysUnexpected[addonIndex] is RequestHandover.AddonName or RequestHandover.ContextAddonName)
            {
                continue;
            }

            if (!AddonReady(alwaysUnexpected[addonIndex]))
            {
                continue;
            }

            addonName = alwaysUnexpected[addonIndex];
            return true;
        }

        if (talkUnexpected && AddonReady(TalkAddonName))
        {
            addonName = TalkAddonName;
            return true;
        }

        addonName = string.Empty;
        return false;
    }

    public static bool CombatOrDutyPop()
        => Svc.Condition[ConditionFlag.InCombat] || AddonReady(DutyPopAddonName);

    private static bool AddonReady(string addonName)
        => GenericHelpers.TryGetAddonByName(addonName, out AtkUnitBase* addon) && GenericHelpers.IsAddonReady(addon);
}
