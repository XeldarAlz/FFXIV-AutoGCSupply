using AutoGcSupply.Core.Value.Model;

namespace AutoGcSupply.Core.Tasks.Model;

internal readonly record struct StartFacts(
    bool Running,
    bool SetupReady,
    bool LoggedIn,
    bool InGrandCompany,
    bool OnHomeWorld,
    bool InDuty,
    bool Loaded,
    PricingState Pricing,
    ShopperState Shopper,
    long SpendCap);

internal static class StartBlockRule
{
    public static StartBlock Evaluate(in StartFacts facts, ReadOnlySpan<TodayRow> rows)
    {
        if (facts.Running)
        {
            return StartBlock.Running;
        }

        if (!facts.SetupReady)
        {
            return StartBlock.SetupNeeded;
        }

        if (facts.Shopper == ShopperState.Outdated)
        {
            return StartBlock.ShopperOutdated;
        }

        if (facts.Shopper == ShopperState.Newer)
        {
            return StartBlock.UpdateNeeded;
        }

        if (!facts.LoggedIn)
        {
            return StartBlock.NotLoggedIn;
        }

        if (!facts.InGrandCompany)
        {
            return StartBlock.NoGrandCompany;
        }

        if (!facts.OnHomeWorld)
        {
            return StartBlock.NotOnHomeWorld;
        }

        if (facts.InDuty)
        {
            return StartBlock.InDuty;
        }

        if (!facts.Loaded)
        {
            return StartBlock.NotLoaded;
        }

        var counts = Count(facts, rows);
        if (counts.Selected == 0)
        {
            return StartBlock.NothingSelected;
        }

        if (counts.Actionable == 0 && counts.AwaitingPrice == 0 && counts.NeedShopper == 0)
        {
            return StartBlock.NothingToDo;
        }

        if (counts.AwaitingPrice > 0)
        {
            return facts.Pricing == PricingState.Failed ? StartBlock.PricesFailed : StartBlock.PricesPending;
        }

        if (counts.Purchases > 0 && facts.SpendCap <= 0)
        {
            return StartBlock.NeedsSpendCap;
        }

        if (counts.Actionable > 0)
        {
            return StartBlock.None;
        }

        return StartBlock.ShopperNeeded;
    }

    private static RowCounts Count(in StartFacts facts, ReadOnlySpan<TodayRow> rows)
    {
        var shopperAvailable = facts.Shopper == ShopperState.Available;
        var pricesOutstanding = facts.Pricing is PricingState.Pending or PricingState.Queued or PricingState.Failed;
        var counts = new RowCounts();
        for (var index = 0; index < rows.Length; index++)
        {
            var decision = rows[index].Decision;
            if (!decision.Included)
            {
                continue;
            }

            counts.Selected++;
            if (decision.NeedsPurchase)
            {
                counts.Purchases++;
                counts.Actionable++;
                continue;
            }

            if (decision.ReadyToHandIn)
            {
                counts.Actionable++;
                continue;
            }

            if (decision.Coverage != RowCoverage.Unpriced)
            {
                continue;
            }

            if (!shopperAvailable)
            {
                counts.NeedShopper++;
            }
            else if (pricesOutstanding)
            {
                counts.AwaitingPrice++;
            }
        }

        return counts;
    }

    private struct RowCounts
    {
        public int Selected;
        public int Actionable;
        public int Purchases;
        public int AwaitingPrice;
        public int NeedShopper;
    }
}
