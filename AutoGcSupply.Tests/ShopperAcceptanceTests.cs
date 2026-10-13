using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Tests;

public sealed class ShopperAcceptanceTests
{
    [Fact]
    public void AcceptedQuoteStartReadsTheQuoteId()
    {
        var acceptance = ShopperReplies.ParseQuoteStart(ShopperFixtures.QuoteStartAccepted);

        Assert.Equal(ShopperReply.Valid, acceptance.Reply);
        Assert.True(acceptance.Accepted);
        Assert.Equal("b6c0...", acceptance.Id);
        Assert.Equal(ShopperRefusal.None, acceptance.Refusal);
        Assert.Equal(string.Empty, acceptance.Message);
    }

    [Fact]
    public void RefusedQuoteStartReadsTheRefusalAndMessage()
    {
        var acceptance = ShopperReplies.ParseQuoteStart(ShopperFixtures.QuoteStartRefused);

        Assert.Equal(ShopperReply.Valid, acceptance.Reply);
        Assert.False(acceptance.Accepted);
        Assert.Equal(string.Empty, acceptance.Id);
        Assert.Equal(ShopperRefusal.InvalidRequest, acceptance.Refusal);
        Assert.Equal("InvalidRequest", acceptance.RefusalName);
        Assert.Equal("localized text", acceptance.Message);
    }

    [Theory]
    [InlineData("UnsupportedSchema")]
    [InlineData("InvalidRequest")]
    [InlineData("LoggedOut")]
    public void QuoteStartReadsEveryListedRefusal(string code)
    {
        var acceptance = ShopperReplies.ParseQuoteStart(Refused(code));

        Assert.False(acceptance.Accepted);
        Assert.Equal(code, acceptance.Refusal.ToString());
    }

    [Fact]
    public void AcceptedStartRunReadsTheRunId()
    {
        var acceptance = ShopperReplies.ParseStartRun(ShopperFixtures.StartRunAccepted);

        Assert.Equal(ShopperReply.Valid, acceptance.Reply);
        Assert.True(acceptance.Accepted);
        Assert.Equal("4e1a...", acceptance.Id);
    }

    [Theory]
    [InlineData("UnsupportedSchema")]
    [InlineData("InvalidRequest")]
    [InlineData("ConsentRequired")]
    [InlineData("ConsentDenied")]
    [InlineData("Busy")]
    [InlineData("SetupNeeded")]
    [InlineData("LoggedOut")]
    [InlineData("JournalDamaged")]
    [InlineData("NeedsReconciliation")]
    [InlineData("InterruptedRun")]
    [InlineData("ReadingUnavailable")]
    [InlineData("BuyingUnavailable")]
    public void StartRunReadsEveryListedRefusal(string code)
    {
        var acceptance = ShopperReplies.ParseStartRun(Refused(code));

        Assert.Equal(ShopperReply.Valid, acceptance.Reply);
        Assert.False(acceptance.Accepted);
        Assert.Equal(code, acceptance.Refusal.ToString());
        Assert.Equal(code, acceptance.RefusalName);
        Assert.Equal("Cannot start: reason.", acceptance.Message);
    }

    [Fact]
    public void RefusalNamesMatchWhateverTheirCase()
    {
        Assert.Equal(ShopperRefusal.ConsentRequired, ShopperReplies.ParseStartRun(Refused("consentrequired")).Refusal);
    }

    [Fact]
    public void UnknownRefusalIsKeptByName()
    {
        var acceptance = ShopperReplies.ParseStartRun(Refused("SomethingNew"));

        Assert.Equal(ShopperReply.Valid, acceptance.Reply);
        Assert.False(acceptance.Accepted);
        Assert.Equal(ShopperRefusal.Unknown, acceptance.Refusal);
        Assert.Equal("SomethingNew", acceptance.RefusalName);
    }

    [Theory]
    [InlineData("""{ "schema": 1, "accepted": false, "runId": null, "refusal": null, "message": null }""", "")]
    [InlineData("""{ "schema": 1, "accepted": false }""", "")]
    [InlineData("""{ "schema": 1, "accepted": false, "refusal": "None" }""", "None")]
    [InlineData("""{ "schema": 1, "accepted": false, "refusal": 4 }""", "4")]
    public void RefusalWithoutAKnownCodeIsAnUnknownRefusal(string json, string name)
    {
        var acceptance = ShopperReplies.ParseStartRun(json);

        Assert.Equal(ShopperReply.Valid, acceptance.Reply);
        Assert.False(acceptance.Accepted);
        Assert.Equal(ShopperRefusal.Unknown, acceptance.Refusal);
        Assert.Equal(name, acceptance.RefusalName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("{")]
    [InlineData("not json")]
    [InlineData("""{ "schema": 1, "accepted": true, "runId": "4e1a..." """)]
    [InlineData("""{ "schema": "1", "accepted": true, "runId": "4e1a..." }""")]
    [InlineData("""{ "schema": 99999999999, "accepted": true, "runId": "4e1a..." }""")]
    [InlineData("""{ "schema": 1, "accepted": "yes", "runId": "4e1a..." }""")]
    [InlineData("""{ "schema": 1, "accepted": true, "runId": 7 }""")]
    [InlineData("""{ "schema": 1, "accepted": false, "refusal": { "code": "Busy" } }""")]
    [InlineData("""{ "accepted": true, "runId": "4e1a..." }""")]
    [InlineData("""{ "schema": null, "accepted": true, "runId": "4e1a..." }""")]
    [InlineData("""{ "schema": 1, "runId": "4e1a..." }""")]
    [InlineData("""{ "schema": 1, "accepted": true }""")]
    [InlineData("""{ "schema": 1, "accepted": true, "runId": "" }""")]
    public void MalformedStartRunRepliesAreMalformed(string json)
    {
        var acceptance = ShopperReplies.ParseStartRun(json);

        Assert.Equal(ShopperReply.Malformed, acceptance.Reply);
        Assert.False(acceptance.Accepted);
        Assert.Equal(ShopperRefusal.None, acceptance.Refusal);
    }

    [Fact]
    public void MalformedQuoteStartReplyIsMalformed()
    {
        Assert.Equal(ShopperReply.Malformed, ShopperReplies.ParseQuoteStart("""{ "schema": 1, "accepted": true }""").Reply);
    }

    [Fact]
    public void AnotherSchemaIsASchemaMismatch()
    {
        Assert.Equal(ShopperReply.SchemaMismatch, ShopperReplies.ParseQuoteStart("""{ "schema": 2, "accepted": true, "quoteId": "b6c0..." }""").Reply);
        Assert.Equal(ShopperReply.SchemaMismatch, ShopperReplies.ParseStartRun("""{ "schema": 2, "somethingElse": [] }""").Reply);
        Assert.False(ShopperReplies.ParseStartRun("""{ "schema": 2, "accepted": true, "runId": "4e1a..." }""").Accepted);
    }

    [Fact]
    public void NoReplyIsUnavailable()
    {
        Assert.Equal(ShopperReply.Unavailable, ShopperReplies.ParseQuoteStart(null).Reply);
        Assert.Equal(ShopperReply.Unavailable, ShopperReplies.ParseStartRun(null).Reply);
        Assert.False(ShopperReplies.ParseStartRun(null).Accepted);
    }

    private static string Refused(string code)
    {
        return $$"""{ "schema": 1, "accepted": false, "runId": null, "quoteId": null, "refusal": "{{code}}", "message": "Cannot start: reason." }""";
    }
}
