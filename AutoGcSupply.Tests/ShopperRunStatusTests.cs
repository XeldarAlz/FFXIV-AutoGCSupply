using System.Text;
using System.Text.Json.Nodes;
using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Tests;

public sealed class ShopperRunStatusTests
{
    private const string GuidRunId = "4e1a0b3c-2d5f-4a6b-8c7d-9e0f1a2b3c4d";

    [Fact]
    public void ExampleReadsEveryField()
    {
        var view = ShopperReplies.ParseRunStatus(ShopperFixtures.RunStatus);

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.Equal("4e1a...", view.RunId);
        Assert.True(view.Known);
        Assert.Equal("0d7f...", view.CallerRunId);
        Assert.Equal(ShopperRunPhase.Shopping, view.Phase);
        Assert.Equal(ShopperRunState.Searching, view.State);
        Assert.Equal("Searching the listings of Darksteel Ore", view.Status);
        Assert.Equal(42, view.Revision);
        Assert.False(view.Paused);
        Assert.Equal(ShopperPauseReason.None, view.PauseReason);
        Assert.False(view.Terminal);
        Assert.Equal(ShopperRunOutcome.None, view.Outcome);
        Assert.False(view.NeedsPlayer);
        Assert.Equal(1890, view.ConfirmedSpend);
        Assert.Equal(0, view.ReservedSpend);
        Assert.Equal(new ShopperRunLine(5057, ShopperQuality.NormalQuality, 3, 3, 0), Assert.Single(view.Lines));
        Assert.Equal(new ShopperRunIssue(ShopperIssueReason.NoStock, 5058), Assert.Single(view.Issues));
        Assert.Equal(ShopperReturnHome.NotNeeded, view.ReturnHome);
        Assert.Equal(ShopperRefusal.None, view.Refusal);
        Assert.Equal(string.Empty, view.RefusalName);
    }

    [Fact]
    public void UnknownRunEchoesTheAskedId()
    {
        var json = $$"""
            {
              "schema": 1, "runId": "{{GuidRunId}}", "known": false, "callerRunId": null,
              "phase": "Idle", "state": "Idle", "status": null, "revision": 0, "paused": false, "pauseReason": null,
              "terminal": false, "outcome": null, "needsPlayer": false, "confirmedSpend": 0, "reservedSpend": 0,
              "lines": [], "issues": [], "returnHome": "NotNeeded", "refusal": null
            }
            """;

        var view = ShopperReplies.ParseRunStatus(json);

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.False(view.Known);
        Assert.Equal(GuidRunId, view.RunId);
        Assert.False(view.Terminal);
        Assert.Empty(view.Lines);
        Assert.Empty(view.Issues);
        Assert.Equal(string.Empty, view.Status);
    }

    [Fact]
    public void UnknownRunNeedsOnlyTheSchemaAndKnown()
    {
        var view = ShopperReplies.ParseRunStatus($$"""{ "schema": 1, "runId": "{{GuidRunId}}", "known": false }""");

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.False(view.Known);
        Assert.Equal(GuidRunId, view.RunId);
        Assert.Empty(view.Lines);
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("CompletedPartial")]
    [InlineData("Stopped")]
    [InlineData("Failed")]
    [InlineData("NeedsReconciliation")]
    public void TerminalRunReadsEveryOutcome(string outcome)
    {
        var view = Parse(root => Terminal(root, outcome));

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.True(view.Terminal);
        Assert.Equal(ShopperRunPhase.Idle, view.Phase);
        Assert.Equal(outcome, view.Outcome.ToString());
    }

    [Fact]
    public void TerminalRunWithoutOutcomeIsMalformed()
    {
        Assert.Equal(ShopperReply.Malformed, Parse(root => root["terminal"] = true).Reply);
    }

    [Fact]
    public void UnrecognizedOutcomeIsAnUnknownOutcome()
    {
        var view = Parse(root => Terminal(root, "Abandoned"));

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.Equal(ShopperRunOutcome.Unknown, view.Outcome);
    }

    [Fact]
    public void UncertainPurchaseEndsAsNeedsReconciliationAndNeedsThePlayer()
    {
        var view = Parse(root =>
        {
            Terminal(root, "NeedsReconciliation");
            root["needsPlayer"] = true;
            root["reservedSpend"] = 630;
        });

        Assert.Equal(ShopperRunOutcome.NeedsReconciliation, view.Outcome);
        Assert.True(view.NeedsPlayer);
        Assert.Equal(630, view.ReservedSpend);
    }

    [Fact]
    public void InterruptedRealRunEndsStoppedAndNeedsThePlayer()
    {
        var view = Parse(root =>
        {
            Terminal(root, "Stopped");
            root["needsPlayer"] = true;
        });

        Assert.True(view.Terminal);
        Assert.Equal(ShopperRunOutcome.Stopped, view.Outcome);
        Assert.True(view.NeedsPlayer);
    }

    [Fact]
    public void WayHomeIsFinishingAndNotYetTerminal()
    {
        var view = Parse(root =>
        {
            root["phase"] = "Finishing";
            root["returnHome"] = "Underway";
        });

        Assert.Equal(ShopperRunPhase.Finishing, view.Phase);
        Assert.False(view.Terminal);
        Assert.Equal(ShopperRunOutcome.None, view.Outcome);
        Assert.Equal(ShopperReturnHome.Underway, view.ReturnHome);
    }

    [Theory]
    [InlineData("SetupNeeded")]
    [InlineData("LoggedOut")]
    [InlineData("JournalDamaged")]
    [InlineData("NeedsReconciliation")]
    [InlineData("InterruptedRun")]
    [InlineData("ReadingUnavailable")]
    [InlineData("BuyingUnavailable")]
    [InlineData("ListLoading")]
    [InlineData("InvalidRequest")]
    [InlineData("PricesUnavailable")]
    [InlineData("PlanFailed")]
    [InlineData("NothingToBuy")]
    [InlineData("AuthorizationFailed")]
    [InlineData("JournalWriteFailed")]
    [InlineData("CharacterChanged")]
    public void PreflightRefusalReadsEveryListedCode(string code)
    {
        var view = Parse(root =>
        {
            Terminal(root, "Stopped");
            root["refusal"] = code;
        });

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.True(view.Terminal);
        Assert.Equal(ShopperRunOutcome.Stopped, view.Outcome);
        Assert.Equal(code, view.Refusal.ToString());
        Assert.Equal(code, view.RefusalName);
    }

    [Fact]
    public void UnknownPreflightRefusalKeepsItsName()
    {
        var view = Parse(root =>
        {
            Terminal(root, "Stopped");
            root["refusal"] = "WorldClosed";
        });

        Assert.Equal(ShopperRefusal.Unknown, view.Refusal);
        Assert.Equal("WorldClosed", view.RefusalName);
    }

    [Theory]
    [InlineData("Manual")]
    [InlineData("InContent")]
    public void PausedRunReadsItsPauseReason(string reason)
    {
        var view = Parse(root =>
        {
            root["phase"] = "Paused";
            root["paused"] = true;
            root["pauseReason"] = reason;
        });

        Assert.True(view.Paused);
        Assert.Equal(ShopperRunPhase.Paused, view.Phase);
        Assert.Equal(reason, view.PauseReason.ToString());
    }

    [Theory]
    [InlineData("NotNeeded")]
    [InlineData("AlreadyHome")]
    [InlineData("Underway")]
    [InlineData("Arrived")]
    [InlineData("Failed")]
    [InlineData("Skipped")]
    public void ReadsEveryReturnHomeName(string returnHome)
    {
        Assert.Equal(returnHome, Parse(root => root["returnHome"] = returnHome).ReturnHome.ToString());
    }

    [Theory]
    [InlineData("Idle")]
    [InlineData("Preparing")]
    [InlineData("Travelling")]
    [InlineData("Shopping")]
    [InlineData("Paused")]
    [InlineData("Finishing")]
    [InlineData("Stopping")]
    public void ReadsEveryPhaseName(string phase)
    {
        Assert.Equal(phase, Parse(root => root["phase"] = phase).Phase.ToString());
    }

    [Fact]
    public void ReadsEveryStateName()
    {
        string[] states =
        [
            "Idle", "Preflight", "FetchingPrices", "Planning", "Ready", "Traveling", "GoingToBoard", "Searching", "Inspecting", "Guarding",
            "PersistingIntent", "Submitting", "AwaitingResult", "Reconciling", "Paused", "NeedsReconciliation", "Completed", "CompletedPartial",
            "Stopped", "Failed",
        ];

        for (var index = 0; index < states.Length; index++)
        {
            var state = states[index];
            Assert.Equal(state, Parse(root => root["state"] = state).State.ToString());
        }
    }

    [Fact]
    public void ReadsEveryIssueReason()
    {
        string[] reasons =
        [
            "PriceCapExceeded", "NoStock", "InventoryFull", "InsufficientGil", "TravelFailed", "DependencyBusy", "UncertainPurchase",
            "UnsupportedGameVersion", "BudgetReached", "SearchFailed", "RetriesExhausted", "ListingRejected", "UniqueOwned",
            "BoardUnreachable", "JournalWriteFailed", "CharacterChanged", "NotMarketable", "CrystalLimit",
        ];
        var issues = new JsonArray();
        for (var index = 0; index < reasons.Length; index++)
        {
            issues.Add(new JsonObject { ["reason"] = reasons[index], ["itemId"] = 5000 + index });
        }

        var view = Parse(root => root["issues"] = issues);

        Assert.Equal(reasons.Length, view.Issues.Length);
        for (var index = 0; index < reasons.Length; index++)
        {
            Assert.Equal(reasons[index], view.Issues[index].Reason.ToString());
            Assert.Equal((uint)(5000 + index), view.Issues[index].ItemId);
        }
    }

    [Fact]
    public void IssueNotAboutOneItemHasNoItem()
    {
        var view = Parse(root => root["issues"] = new JsonArray(new JsonObject { ["reason"] = "BudgetReached" }, new JsonObject { ["reason"] = "BudgetReached", ["itemId"] = 0 }));

        Assert.Equal(new ShopperRunIssue(ShopperIssueReason.BudgetReached, 0), view.Issues[0]);
        Assert.Equal(new ShopperRunIssue(ShopperIssueReason.BudgetReached, 0), view.Issues[1]);
    }

    [Fact]
    public void UnrecognizedNamesAreUnknownWithoutFailingTheReply()
    {
        var view = Parse(root =>
        {
            root["phase"] = "Dancing";
            root["state"] = 7;
            root["pauseReason"] = "Weather";
            root["returnHome"] = "Teleported";
            root["issues"] = new JsonArray(new JsonObject { ["reason"] = "Thunderstorm", ["itemId"] = 5058 });
        });

        Assert.Equal(ShopperReply.Valid, view.Reply);
        Assert.Equal(ShopperRunPhase.Unknown, view.Phase);
        Assert.Equal(ShopperRunState.Unknown, view.State);
        Assert.Equal(ShopperPauseReason.Unknown, view.PauseReason);
        Assert.Equal(ShopperReturnHome.Unknown, view.ReturnHome);
        Assert.Equal(ShopperIssueReason.Unknown, view.Issues[0].Reason);
    }

    [Fact]
    public void NamesAndPropertiesMatchWhateverTheirCase()
    {
        var json = ShopperFixtures.RunStatus.Replace("\"revision\"", "\"Revision\"", StringComparison.Ordinal).Replace("\"Shopping\"", "\"shopping\"", StringComparison.Ordinal);

        var view = ShopperReplies.ParseRunStatus(json);

        Assert.Equal(42, view.Revision);
        Assert.Equal(ShopperRunPhase.Shopping, view.Phase);
    }

    [Fact]
    public void LineWithoutQualityIsAny()
    {
        var view = Parse(root => root["lines"]![0]!.AsObject().Remove("quality"));

        Assert.Equal(ShopperQuality.Any, view.Lines[0].Quality);
    }

    [Fact]
    public void LinesKeepTheRequestOrder()
    {
        var view = Parse(root => root["lines"] = new JsonArray(
            new JsonObject { ["itemId"] = 5058, ["quality"] = "HighQuality", ["requested"] = 2, ["acquired"] = 0, ["missing"] = 2 },
            new JsonObject { ["itemId"] = 5058, ["quality"] = "NormalQuality", ["requested"] = 1, ["acquired"] = 1, ["missing"] = 0 }));

        Assert.Equal(new ShopperRunLine(5058, ShopperQuality.HighQuality, 2, 0, 2), view.Lines[0]);
        Assert.Equal(new ShopperRunLine(5058, ShopperQuality.NormalQuality, 1, 1, 0), view.Lines[1]);
    }

    [Theory]
    [InlineData("revision")]
    [InlineData("known")]
    [InlineData("paused")]
    [InlineData("terminal")]
    [InlineData("needsPlayer")]
    [InlineData("confirmedSpend")]
    [InlineData("reservedSpend")]
    [InlineData("lines")]
    [InlineData("schema")]
    public void KnownRunWithoutARequiredFieldIsMalformed(string property)
    {
        Assert.Equal(ShopperReply.Malformed, Parse(root => root.Remove(property)).Reply);
    }

    [Theory]
    [InlineData("itemId")]
    [InlineData("requested")]
    [InlineData("acquired")]
    [InlineData("missing")]
    public void LineWithoutARequiredFieldIsMalformed(string property)
    {
        Assert.Equal(ShopperReply.Malformed, Parse(root => root["lines"]![0]!.AsObject().Remove(property)).Reply);
    }

    [Fact]
    public void LineWithAnUnrecognizedQualityIsMalformed()
    {
        Assert.Equal(ShopperReply.Malformed, Parse(root => root["lines"]![0]!["quality"] = "Shiny").Reply);
    }

    [Fact]
    public void NegativeCountsAreMalformed()
    {
        Assert.Equal(ShopperReply.Malformed, Parse(root => root["lines"]![0]!["missing"] = -1).Reply);
        Assert.Equal(ShopperReply.Malformed, Parse(root => root["confirmedSpend"] = -1).Reply);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("not json")]
    [InlineData("""{ "schema": 1, "known": "yes" }""")]
    [InlineData("""{ "schema": 1, "known": true, "revision": "42" }""")]
    [InlineData("""{ "schema": 1, "known": true, "lines": [ 5057 ] }""")]
    public void MalformedRunRepliesAreMalformed(string json)
    {
        var view = ShopperReplies.ParseRunStatus(json);

        Assert.Equal(ShopperReply.Malformed, view.Reply);
        Assert.Empty(view.Lines);
        Assert.Empty(view.Issues);
    }

    [Fact]
    public void AnotherSchemaIsASchemaMismatch()
    {
        var view = Parse(root => root["schema"] = 2);

        Assert.Equal(ShopperReply.SchemaMismatch, view.Reply);
        Assert.False(view.Known);
        Assert.Empty(view.Lines);
    }

    [Fact]
    public void NoReplyIsUnavailable()
    {
        var view = ShopperReplies.ParseRunStatus((string?)null);

        Assert.Equal(ShopperReply.Unavailable, view.Reply);
        Assert.Empty(view.Lines);
        Assert.Equal(string.Empty, view.Status);
    }

    [Fact]
    public void Utf8ReplyReadsLikeTheText()
    {
        var fromText = ShopperReplies.ParseRunStatus(ShopperFixtures.RunStatus);
        var fromUtf8 = ShopperReplies.ParseRunStatus(Encoding.UTF8.GetBytes(ShopperFixtures.RunStatus));

        Assert.Equal(fromText.Revision, fromUtf8.Revision);
        Assert.Equal(fromText.Status, fromUtf8.Status);
        Assert.Equal(fromText.Lines[0], fromUtf8.Lines[0]);
        Assert.Equal(fromText.Issues[0], fromUtf8.Issues[0]);
    }

    private static ShopperRunView Parse(Action<JsonObject> edit)
    {
        return ShopperReplies.ParseRunStatus(ShopperJsonEdit.Edit(ShopperFixtures.RunStatus, edit));
    }

    private static void Terminal(JsonObject root, string outcome)
    {
        root["terminal"] = true;
        root["outcome"] = outcome;
        root["phase"] = "Idle";
    }
}
