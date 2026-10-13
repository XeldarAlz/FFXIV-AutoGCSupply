using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Tests;

internal static class PriceFixtures
{
    public static readonly QualityPrice None = new(false, 0, 0, 0);

    public static QualityPrice Listed(int units, long total) => new(true, units, total, (total + units - 1) / units);

    public static QualityPrice PerUnit(int units, long unitPrice) => new(true, units, unitPrice * units, unitPrice);

    public static ItemPrice Item(uint itemId, int quantity, QualityPrice normal, QualityPrice high) => new(itemId, quantity, normal, high);
}
