using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class BuyProgress
{
    private const uint RunWideIssue = 0;

    public static int Apply(ReadOnlySpan<RowDecision> plan, Span<RowProgress> progress, in ShopperRunView view)
    {
        var changed = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            var row = plan[index];
            if (!row.NeedsPurchase)
            {
                continue;
            }

            var quality = ShopperLineBuilder.QualityOf(row.ChosenQuality);
            var claimedEarlier = ClaimedBefore(plan, index, row.Request.ItemId, quality);
            var acquired = Acquired(view.Lines, row.Request.ItemId, quality);
            var bought = Math.Clamp(acquired - claimedEarlier, 0, row.BuyQuantity);
            var next = progress[index] with { Bought = bought };
            if (bought >= row.BuyQuantity)
            {
                next = next.With(RowRunState.Bought, RowNote.Bought);
            }
            else if (view.Terminal)
            {
                next = next.With(RowRunState.NotBought, NoteFor(view.Issues, row.Request.ItemId));
            }
            else
            {
                next = next.With(RowRunState.Buying, RowNote.Buying);
            }

            if (next == progress[index])
            {
                continue;
            }

            progress[index] = next;
            changed++;
        }

        return changed;
    }

    public static RowNote NoteFor(ImmutableArray<ShopperRunIssue> issues, uint itemId)
    {
        var runWide = RowNote.ShopperIssue;
        for (var index = 0; index < issues.Length; index++)
        {
            if (issues[index].ItemId == itemId)
            {
                return NoteOf(issues[index].Reason);
            }

            if (issues[index].ItemId == RunWideIssue && runWide == RowNote.ShopperIssue)
            {
                runWide = NoteOf(issues[index].Reason);
            }
        }

        return runWide;
    }

    private static RowNote NoteOf(ShopperIssueReason reason) => reason switch
    {
        ShopperIssueReason.PriceCapExceeded => RowNote.PriceCapExceeded,
        ShopperIssueReason.NoStock => RowNote.NoStock,
        ShopperIssueReason.InventoryFull => RowNote.InventoryFull,
        ShopperIssueReason.InsufficientGil => RowNote.InsufficientGil,
        ShopperIssueReason.BudgetReached => RowNote.BudgetReached,
        ShopperIssueReason.NotMarketable => RowNote.NotMarketable,
        _ => RowNote.ShopperIssue,
    };

    private static int ClaimedBefore(ReadOnlySpan<RowDecision> plan, int rowIndex, uint itemId, ShopperQuality quality)
    {
        var claimed = 0;
        for (var index = 0; index < rowIndex; index++)
        {
            var row = plan[index];
            if (row.NeedsPurchase && row.Request.ItemId == itemId && ShopperLineBuilder.QualityOf(row.ChosenQuality) == quality)
            {
                claimed += row.BuyQuantity;
            }
        }

        return claimed;
    }

    private static int Acquired(ImmutableArray<ShopperRunLine> lines, uint itemId, ShopperQuality quality)
    {
        var acquired = 0;
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].ItemId == itemId && lines[index].Quality == quality)
            {
                acquired += lines[index].Acquired;
            }
        }

        return acquired;
    }
}
