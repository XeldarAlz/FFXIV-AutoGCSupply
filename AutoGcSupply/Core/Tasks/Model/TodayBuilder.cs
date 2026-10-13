using AutoGcSupply.Core.Requests.Model;
using AutoGcSupply.Core.Shopper.Model;
using AutoGcSupply.Core.Value.Model;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AutoGcSupply.Core.Tasks.Model;

internal static class TodayBuilder
{
    public static TodayModel Build(in TodayInputs inputs, QuoteBook quotes, IRowLabels labels, int revision, out ShopperQuoteLine[] neededQuote)
    {
        var requestsLoaded = inputs.Snapshot.Loaded && inputs.RequestsCurrent;
        var decisions = DecisionsOf(inputs, requestsLoaded, quotes.Prices);
        var rows = RowsOf(inputs, decisions, labels);
        neededQuote = QuoteLines.ForDecisions(decisions);
        var shopperAvailable = inputs.Shopper == ShopperState.Available;
        var pricing = quotes.StateFor(neededQuote, shopperAvailable);
        var totals = TodayTotalsRule.Sum(rows.AsSpan(), inputs.Character.Seals, inputs.Character.SealCap);
        var loggedIn = inputs.LoggedIn && inputs.HasCharacter;
        var facts = new StartFacts(
            inputs.Running,
            inputs.SetupReady,
            loggedIn,
            loggedIn && inputs.Character.InGrandCompany,
            loggedIn && inputs.Character.OnHomeWorld,
            inputs.InDuty,
            requestsLoaded && !inputs.Reading,
            pricing,
            inputs.Shopper,
            inputs.SpendCap);
        return new TodayModel
        {
            Revision = revision,
            State = StateOf(inputs, requestsLoaded),
            Header = HeaderOf(inputs),
            Rows = rows,
            Totals = totals,
            Shopper = inputs.Shopper,
            ShopperDryRun = shopperAvailable && inputs.ShopperDryRun,
            Pricing = pricing,
            SpendCap = inputs.SpendCap,
            SuggestedSpendCap = SpendCapRule.Suggest(totals.EstimatedSpend),
            StartBlock = StartBlockRule.Evaluate(facts, rows.AsSpan()),
        };
    }

    private static TodayState StateOf(in TodayInputs inputs, bool requestsLoaded)
    {
        if (!inputs.LoggedIn || !inputs.HasCharacter)
        {
            return TodayState.NotLoggedIn;
        }

        if (!inputs.Character.InGrandCompany)
        {
            return TodayState.NoGrandCompany;
        }

        if (inputs.Reading)
        {
            return TodayState.Reading;
        }

        if (!requestsLoaded)
        {
            return TodayState.NotLoaded;
        }

        return inputs.Snapshot.AllDone ? TodayState.AllDone : TodayState.Ready;
    }

    private static TodayHeader HeaderOf(in TodayInputs inputs)
    {
        var day = DayKey.For(inputs.NowUtc);
        if (!inputs.HasCharacter)
        {
            return new TodayHeader(0, 0, 0, 0, day.NextResetUtc, day.ToString());
        }

        var character = inputs.Character;
        return new TodayHeader(character.GrandCompany, character.Rank, character.Seals, character.SealCap, day.NextResetUtc, day.ToString());
    }

    private static RowDecision[] DecisionsOf(in TodayInputs inputs, bool requestsLoaded, ReadOnlySpan<ItemPrice> prices)
    {
        if (!inputs.HasCharacter || !requestsLoaded)
        {
            return [];
        }

        return DecisionBuilder.Build(inputs.Snapshot, inputs.Character, inputs.OwnedBySlot.AsSpan(), prices, inputs.Settings);
    }

    private static ImmutableArray<TodayRow> RowsOf(in TodayInputs inputs, RowDecision[] decisions, IRowLabels labels)
    {
        var rows = new TodayRow[decisions.Length];
        for (var index = 0; index < decisions.Length; index++)
        {
            var decision = decisions[index];
            var classJobId = decision.Request.ClassJobId;
            var itemId = decision.Request.ItemId;
            rows[index] = new TodayRow(
                decision,
                labels.ClassName(classJobId),
                inputs.Character.Class(classJobId).Level,
                labels.ItemName(itemId),
                labels.ItemIconId(itemId));
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(rows);
    }
}
