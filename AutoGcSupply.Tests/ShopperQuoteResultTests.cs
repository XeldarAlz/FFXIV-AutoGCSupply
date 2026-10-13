using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Tests;

public sealed class ShopperQuoteResultTests
{
    [Fact]
    public void ReadyExampleReadsEveryPrice()
    {
        var quote = ShopperReplies.ParseQuoteResult(ShopperFixtures.QuoteResult);

        Assert.Equal(ShopperReply.Valid, quote.Reply);
        Assert.Equal("b6c0...", quote.QuoteId);
        Assert.Equal(ShopperQuoteState.Ready, quote.State);
        Assert.Equal(ShopperRefusal.None, quote.Refusal);
        Assert.Equal(new DateTime(2026, 10, 13, 18, 22, 5, DateTimeKind.Utc), quote.FetchedAtUtc);
        Assert.Equal(DateTimeKind.Utc, quote.FetchedAtUtc.Kind);
        var item = Assert.Single(quote.Items);
        Assert.Equal(5057u, item.ItemId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(new ShopperQuotePrice(true, 3, 1890, 630), item.Normal);
        Assert.Equal(new ShopperQuotePrice(false, 0, 0, 0), item.High);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Unknown")]
    public void QuoteThatIsNotReadyHasNoItems(string state)
    {
        var quote = ShopperReplies.ParseQuoteResult(NotReady(state, "null"));

        Assert.Equal(ShopperReply.Valid, quote.Reply);
        Assert.Equal(state, quote.State.ToString());
        Assert.Empty(quote.Items);
        Assert.Equal(default, quote.FetchedAtUtc);
        Assert.Equal(ShopperRefusal.None, quote.Refusal);
    }

    [Fact]
    public void FailedQuoteReadsWhyThePricesAreMissing()
    {
        var quote = ShopperReplies.ParseQuoteResult(NotReady("Failed", "\"PricesUnavailable\""));

        Assert.Equal(ShopperReply.Valid, quote.Reply);
        Assert.Equal(ShopperQuoteState.Failed, quote.State);
        Assert.Equal(ShopperRefusal.PricesUnavailable, quote.Refusal);
        Assert.Equal("PricesUnavailable", quote.RefusalName);
        Assert.Empty(quote.Items);
    }

    [Fact]
    public void StateNamesMatchWhateverTheirCase()
    {
        var quote = ShopperReplies.ParseQuoteResult(ShopperJsonEdit.Edit(ShopperFixtures.QuoteResult, root => root["state"] = "ready"));

        Assert.Equal(ShopperQuoteState.Ready, quote.State);
        Assert.Single(quote.Items);
    }

    [Fact]
    public void ItemsAreReadOnlyWhenTheQuoteIsReady()
    {
        var quote = ShopperReplies.ParseQuoteResult(ShopperJsonEdit.Edit(ShopperFixtures.QuoteResult, root => root["state"] = "Pending"));

        Assert.Equal(ShopperQuoteState.Pending, quote.State);
        Assert.Empty(quote.Items);
    }

    [Theory]
    [InlineData("""{ "schema": 1, "quoteId": "b6c0...", "state": "Later", "items": [] }""")]
    [InlineData("""{ "schema": 1, "quoteId": "b6c0...", "state": 1, "items": [] }""")]
    [InlineData("""{ "schema": 1, "quoteId": "b6c0...", "items": [] }""")]
    [InlineData("""{ "schema": 1, "quoteId": "b6c0...", "state": "Ready", "fetchedAtUtc": "yesterday", "items": [] }""")]
    [InlineData("""{ "quoteId": "b6c0...", "state": "Pending", "items": [] }""")]
    [InlineData("""{ "schema": 1, "quoteId": "b6c0...", "state": "Pending", "items": {} }""")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{")]
    public void MalformedQuoteRepliesAreMalformed(string json)
    {
        var quote = ShopperReplies.ParseQuoteResult(json);

        Assert.Equal(ShopperReply.Malformed, quote.Reply);
        Assert.Empty(quote.Items);
    }

    [Fact]
    public void ReadyQuoteWithoutItemsIsMalformed()
    {
        AssertMalformed(root => root.Remove("items"));
    }

    [Fact]
    public void ReadyQuoteWithoutFetchTimeIsMalformed()
    {
        AssertMalformed(root => root["fetchedAtUtc"] = null);
    }

    [Fact]
    public void ReadyQuoteItemWithoutOneQualityIsMalformed()
    {
        AssertMalformed(root => root["items"]![0]!.AsObject().Remove("high"));
    }

    [Fact]
    public void ReadyQuoteItemWithoutItsIdIsMalformed()
    {
        AssertMalformed(root => root["items"]![0]!.AsObject().Remove("itemId"));
    }

    [Fact]
    public void PriceWithoutAvailabilityIsMalformed()
    {
        AssertMalformed(root => root["items"]![0]!["normal"]!.AsObject().Remove("available"));
    }

    [Fact]
    public void NegativePriceIsMalformed()
    {
        AssertMalformed(root => root["items"]![0]!["normal"]!["total"] = -1);
    }

    [Fact]
    public void AnotherSchemaIsASchemaMismatch()
    {
        var quote = ShopperReplies.ParseQuoteResult(ShopperJsonEdit.Edit(ShopperFixtures.QuoteResult, root => root["schema"] = 2));

        Assert.Equal(ShopperReply.SchemaMismatch, quote.Reply);
        Assert.Empty(quote.Items);
    }

    [Fact]
    public void NoReplyIsUnavailable()
    {
        var quote = ShopperReplies.ParseQuoteResult(null);

        Assert.Equal(ShopperReply.Unavailable, quote.Reply);
        Assert.Empty(quote.Items);
    }

    private static void AssertMalformed(Action<System.Text.Json.Nodes.JsonObject> edit)
    {
        var quote = ShopperReplies.ParseQuoteResult(ShopperJsonEdit.Edit(ShopperFixtures.QuoteResult, edit));

        Assert.Equal(ShopperReply.Malformed, quote.Reply);
        Assert.Empty(quote.Items);
    }

    private static string NotReady(string state, string refusal)
    {
        return $$"""{ "schema": 1, "quoteId": "b6c0...", "state": "{{state}}", "refusal": {{refusal}}, "fetchedAtUtc": null, "items": [] }""";
    }
}
