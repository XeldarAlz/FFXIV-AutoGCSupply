using AutoGcSupply.Core.HandIn.Model;
using ECommons;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class RewardWindowReader
{
    private const int ItemNameIndex = 4;
    private const int SealRewardIndex = 9;

    public static bool Readable() => TryGetWindow(out var window) && ValuesReadable(window);

    public static RewardWindowSample Read()
    {
        if (!TryGetWindow(out var window) || !ValuesReadable(window))
        {
            return RewardWindowSample.Unreadable;
        }

        var values = window->AtkValues;
        return new RewardWindowSample(true, AtkValueReads.Text(values[ItemNameIndex]), AtkValueReads.Integer(values[SealRewardIndex]));
    }

    public static string Describe() => TryGetWindow(out var window) ? AtkValueReads.Describe(window) : "the reward window is not open";

    private static bool TryGetWindow(out AtkUnitBase* window)
        => GenericHelpers.TryGetAddonByName(HandInClicks.RewardAddonName, out window) && GenericHelpers.IsAddonReady(window);

    private static bool ValuesReadable(AtkUnitBase* window)
        => window->AtkValues != null
        && window->AtkValuesCount > SealRewardIndex
        && AtkValueReads.IsText(window->AtkValues[ItemNameIndex])
        && AtkValueReads.IsInteger(window->AtkValues[SealRewardIndex]);
}
