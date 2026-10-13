using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class RunPlanRules
{
    public static RowProgress[] Initial(ReadOnlySpan<RowDecision> rows)
    {
        var progress = new RowProgress[rows.Length];
        for (var index = 0; index < rows.Length; index++)
        {
            progress[index] = rows[index].Coverage switch
            {
                RowCoverage.ToBuy => RowProgress.Of(RowRunState.Waiting, RowNote.ToBuy),
                RowCoverage.Unpriced => RowProgress.Of(RowRunState.NotBought, RowNote.NoPrice),
                RowCoverage.NoListings => RowProgress.Of(RowRunState.NotBought, RowNote.NoListings),
                _ => RowProgress.Of(RowRunState.Waiting, RowNote.InBags),
            };
        }

        return progress;
    }

    public static int DropChanged(Span<RowDecision> plan, Span<RowProgress> progress, in RequestSnapshot snapshot)
    {
        var dropped = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            var row = plan[index];
            if (!row.Included)
            {
                continue;
            }

            var stillMatches = snapshot.TryFind(row.Request.ClassJobId, out var request)
                && request.ItemId == row.Request.ItemId
                && request.Requested == row.Request.Requested;
            var state = progress[index].State;
            if (stillMatches && request.Open)
            {
                if (state == RowRunState.HandingIn)
                {
                    progress[index] = progress[index].With(RowRunState.Waiting, RowNote.InBags);
                }

                continue;
            }

            plan[index] = row with { Included = false };
            if (state == RowRunState.Delivered)
            {
                continue;
            }

            if (stillMatches && state == RowRunState.HandingIn)
            {
                progress[index] = progress[index].With(RowRunState.Delivered, RowNote.HandedIn);
                continue;
            }

            progress[index] = progress[index].With(RowRunState.Skipped, stillMatches ? RowNote.AlreadyHandedIn : RowNote.RequestChanged);
            dropped++;
        }

        return dropped;
    }

    public static int ApplyFreshPrices(Span<RowDecision> plan, Span<RowProgress> progress, ReadOnlySpan<ItemPrice> prices, int tolerancePercent)
    {
        var changed = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            var row = plan[index];
            if (!row.NeedsPurchase || !ItemPrice.TryFind(prices, row.Request.ItemId, out var price))
            {
                continue;
            }

            var quality = price.Of(row.ChosenQuality);
            if (!quality.Usable)
            {
                plan[index] = row with { Coverage = RowCoverage.NoListings };
                progress[index] = progress[index].With(RowRunState.NotBought, RowNote.NoListings);
                changed++;
                continue;
            }

            if (CapRule.TryMaxUnitPrice(quality.UnitPrice, quality.HighestUnitPrice, tolerancePercent, out var freshCap) && freshCap < row.MaxUnitPrice)
            {
                plan[index] = row with { MaxUnitPrice = freshCap };
                changed++;
            }
        }

        return changed;
    }

    public static int MarkUnbought(ReadOnlySpan<RowDecision> plan, Span<RowProgress> progress, RowNote note)
    {
        var marked = 0;
        for (var index = 0; index < plan.Length; index++)
        {
            if (!plan[index].NeedsPurchase || progress[index].State == RowRunState.NotBought)
            {
                continue;
            }

            progress[index] = progress[index].With(RowRunState.NotBought, note);
            marked++;
        }

        return marked;
    }

    public static int ReleaseHandingIn(Span<RowProgress> progress)
    {
        var released = 0;
        for (var index = 0; index < progress.Length; index++)
        {
            if (progress[index].State != RowRunState.HandingIn)
            {
                continue;
            }

            progress[index] = progress[index].With(RowRunState.Waiting, RowNote.InBags);
            released++;
        }

        return released;
    }

    public static bool NeedsTimersPass(bool currentToday, bool handInStarted, in RequestSnapshot memory)
        => !currentToday && !(handInStarted && memory.AllDone);

    public static bool AnyPurchase(ReadOnlySpan<RowDecision> plan)
    {
        for (var index = 0; index < plan.Length; index++)
        {
            if (plan[index].NeedsPurchase)
            {
                return true;
            }
        }

        return false;
    }
}
