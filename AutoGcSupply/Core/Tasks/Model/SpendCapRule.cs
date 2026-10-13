using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class SpendCapRule
{
    public const long Maximum = ShopperContract.MaxSpend;

    private const long HeadroomPercent = 120;
    private const long PercentDenominator = 100;
    private const long SmallestStep = 100;
    private const long SignificantRange = 100;
    private const long DecimalBase = 10;

    public static long Clamp(long gil) => Math.Clamp(gil, 0, Maximum);

    public static long Suggest(long estimatedSpend)
    {
        if (estimatedSpend <= 0)
        {
            return 0;
        }

        var withHeadroom = ((Int128)estimatedSpend * HeadroomPercent + PercentDenominator - 1) / PercentDenominator;
        if (withHeadroom >= Maximum)
        {
            return Maximum;
        }

        var value = (long)withHeadroom;
        var step = SmallestStep;
        while (value / step >= SignificantRange)
        {
            step *= DecimalBase;
        }

        return Math.Min((value + step - 1) / step * step, Maximum);
    }
}
