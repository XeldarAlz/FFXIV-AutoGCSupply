namespace AutoGcSupply.Core.Value.Model;

public static class CapRule
{
    private const long PercentDenominator = 100;

    public static bool TryMaxUnitPrice(long unitPrice, long highestUnitPrice, int tolerancePercent, out long maxUnitPrice)
    {
        if (unitPrice <= 0 || tolerancePercent < 0)
        {
            maxUnitPrice = 0;
            return false;
        }

        var basePrice = Math.Max(unitPrice, highestUnitPrice);
        var scaled = (Int128)basePrice * (PercentDenominator + tolerancePercent);
        var roundedUp = (scaled + PercentDenominator - 1) / PercentDenominator;
        if (roundedUp > long.MaxValue)
        {
            maxUnitPrice = 0;
            return false;
        }

        maxUnitPrice = (long)roundedUp;
        return true;
    }
}
