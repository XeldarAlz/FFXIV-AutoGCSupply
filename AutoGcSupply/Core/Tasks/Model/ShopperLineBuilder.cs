using AutoGcSupply.Core.Inventory.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class ShopperLineBuilder
{
    public static ShopperQuality QualityOf(ItemQuality quality)
        => quality == ItemQuality.High ? ShopperQuality.HighQuality : ShopperQuality.NormalQuality;

    public static ShopperLine[] Build(ReadOnlySpan<RowDecision> plan)
    {
        Span<ShopperLine> lines = stackalloc ShopperLine[plan.Length];
        var count = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            var row = plan[index];
            if (!row.NeedsPurchase)
            {
                continue;
            }

            var line = new ShopperLine(
                row.Request.ItemId,
                QualityOf(row.ChosenQuality),
                row.BuyQuantity,
                Math.Min(row.MaxUnitPrice, ShopperContract.MaxUnitPrice),
                Math.Min(row.ExcessAllowance, ShopperContract.MaxExcessAllowance));
            count = Merge(lines, count, line);
        }

        return lines[..count].ToArray();
    }

    private static int Merge(Span<ShopperLine> lines, int count, in ShopperLine line)
    {
        for (var lineIndex = 0; lineIndex < count; lineIndex++)
        {
            var existing = lines[lineIndex];
            if (existing.ItemId != line.ItemId || existing.Quality != line.Quality)
            {
                continue;
            }

            lines[lineIndex] = existing with
            {
                Quantity = existing.Quantity + line.Quantity,
                MaxUnitPrice = Math.Min(existing.MaxUnitPrice, line.MaxUnitPrice),
                ExcessAllowance = Math.Min(existing.ExcessAllowance + line.ExcessAllowance, ShopperContract.MaxExcessAllowance),
            };
            return count;
        }

        lines[count] = line;
        return count + 1;
    }
}
