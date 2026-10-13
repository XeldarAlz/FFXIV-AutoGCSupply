using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class QuoteLines
{
    public static ShopperQuoteLine[] ForDecisions(ReadOnlySpan<RowDecision> decisions)
    {
        Span<ShopperQuoteLine> lines = stackalloc ShopperQuoteLine[decisions.Length];
        var count = 0;
        for (var index = 0; index < decisions.Length; index++)
        {
            var decision = decisions[index];
            if (decision.Included && decision.Coverage is RowCoverage.ToBuy or RowCoverage.Unpriced or RowCoverage.NoListings)
            {
                count = Merge(lines, count, decision.Request.ItemId, decision.Request.Requested);
            }
        }

        return lines[..count].ToArray();
    }

    public static ShopperQuoteLine[] ForPurchases(ReadOnlySpan<RowDecision> plan)
    {
        Span<ShopperQuoteLine> lines = stackalloc ShopperQuoteLine[plan.Length];
        var count = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            if (plan[index].NeedsPurchase)
            {
                count = Merge(lines, count, plan[index].Request.ItemId, plan[index].Request.Requested);
            }
        }

        return lines[..count].ToArray();
    }

    public static bool Covers(ReadOnlySpan<ShopperQuoteLine> quoted, ReadOnlySpan<ShopperQuoteLine> needed)
    {
        for (var neededIndex = 0; neededIndex < needed.Length; neededIndex++)
        {
            if (!Quotes(quoted, needed[neededIndex]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Quotes(ReadOnlySpan<ShopperQuoteLine> quoted, in ShopperQuoteLine line)
    {
        for (var quotedIndex = 0; quotedIndex < quoted.Length; quotedIndex++)
        {
            if (quoted[quotedIndex].ItemId == line.ItemId && quoted[quotedIndex].Quantity >= line.Quantity)
            {
                return true;
            }
        }

        return false;
    }

    private static int Merge(Span<ShopperQuoteLine> lines, int count, uint itemId, int quantity)
    {
        for (var lineIndex = 0; lineIndex < count; lineIndex++)
        {
            if (lines[lineIndex].ItemId == itemId)
            {
                lines[lineIndex] = lines[lineIndex] with { Quantity = lines[lineIndex].Quantity + quantity };
                return count;
            }
        }

        lines[count] = new ShopperQuoteLine(itemId, quantity);
        return count + 1;
    }
}
