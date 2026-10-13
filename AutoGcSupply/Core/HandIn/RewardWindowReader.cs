using AutoGcSupply.Core.HandIn.Model;
using ECommons;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoGcSupply.Core.HandIn;

internal static unsafe class RewardWindowReader
{
    public static bool Readable() => TryGetWindow(out var window) && HasValues(window);

    public static RewardWindowSample Read()
    {
        if (!TryGetWindow(out var window) || !HasValues(window))
        {
            return RewardWindowSample.Unreadable;
        }

        var values = window->AtkValues;
        var valueCount = (int)window->AtkValuesCount;
        var textCount = 0;
        var integerCount = 0;
        for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
        {
            if (AtkValueReads.IsText(values[valueIndex]))
            {
                textCount++;
            }
            else if (AtkValueReads.IsInteger(values[valueIndex]))
            {
                integerCount++;
            }
        }

        var texts = new RewardWindowText[textCount];
        var integers = new RewardWindowInteger[integerCount];
        var textPosition = 0;
        var integerPosition = 0;
        for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
        {
            ref readonly var value = ref values[valueIndex];
            if (AtkValueReads.IsText(value))
            {
                texts[textPosition++] = new RewardWindowText(valueIndex, AtkValueReads.Text(value));
            }
            else if (AtkValueReads.IsInteger(value))
            {
                integers[integerPosition++] = new RewardWindowInteger(valueIndex, AtkValueReads.Integer(value));
            }
        }

        return new RewardWindowSample(true, texts, integers);
    }

    public static string Describe() => TryGetWindow(out var window) ? AtkValueReads.Describe(window) : "the reward window is not open";

    private static bool TryGetWindow(out AtkUnitBase* window)
        => GenericHelpers.TryGetAddonByName(HandInClicks.RewardAddonName, out window) && GenericHelpers.IsAddonReady(window);

    private static bool HasValues(AtkUnitBase* window) => window->AtkValues != null && window->AtkValuesCount > 0;
}
