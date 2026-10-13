using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Tests;

public sealed class ShopperJsonRoundTripTests
{
    [Fact]
    public void QuoteStartRequestRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.QuoteStartRequest, ShopperJsonContext.Default.QuoteStartRequestJson);
    }

    [Fact]
    public void AcceptedQuoteStartResponseRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.QuoteStartAccepted, ShopperJsonContext.Default.QuoteStartResponseJson);
    }

    [Fact]
    public void RefusedQuoteStartResponseRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.QuoteStartRefused, ShopperJsonContext.Default.QuoteStartResponseJson);
    }

    [Fact]
    public void QuoteResultResponseRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.QuoteResult, ShopperJsonContext.Default.QuoteResultResponseJson);
    }

    [Fact]
    public void QuoteResultWithTheHighestUnitPriceRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.QuoteResultWithHighest, ShopperJsonContext.Default.QuoteResultResponseJson);
    }

    [Fact]
    public void StartRunRequestRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.StartRunRequest, ShopperJsonContext.Default.StartRunRequestJson);
    }

    [Fact]
    public void StartRunResponseRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.StartRunAccepted, ShopperJsonContext.Default.StartRunResponseJson);
    }

    [Fact]
    public void RunStatusResponseRoundTrips()
    {
        AssertRoundTrip(ShopperFixtures.RunStatus, ShopperJsonContext.Default.RunStatusResponseJson);
    }

    [Fact]
    public void StartRunRequestWritesQualityAsItsName()
    {
        var request = JsonSerializer.Deserialize(ShopperFixtures.StartRunRequest, ShopperJsonContext.Default.StartRunRequestJson);

        Assert.Equal(ShopperQuality.NormalQuality, request.Lines[0].Quality);
        Assert.Contains("\"quality\":\"NormalQuality\"", JsonSerializer.Serialize(request, ShopperJsonContext.Default.StartRunRequestJson), StringComparison.Ordinal);
    }

    [Fact]
    public void ResponsesReadPropertyNamesWhateverTheirCase()
    {
        var response = JsonSerializer.Deserialize("""{ "Schema": 1, "ACCEPTED": true, "runid": "4e1a..." }""", ShopperJsonContext.Default.StartRunResponseJson);

        Assert.Equal(1, response.Schema);
        Assert.True(response.Accepted);
        Assert.Equal("4e1a...", response.RunId);
    }

    [Fact]
    public void ResponsesIgnoreUnknownProperties()
    {
        var response = JsonSerializer.Deserialize("""{ "schema": 1, "accepted": true, "runId": "4e1a...", "future": { "nested": [1, 2] } }""", ShopperJsonContext.Default.StartRunResponseJson);

        Assert.Equal("4e1a...", response.RunId);
    }

    [Fact]
    public void NamesGivenAsNumbersAreReadAsTheirText()
    {
        var response = JsonSerializer.Deserialize("""{ "schema": 1, "accepted": false, "refusal": 4 }""", ShopperJsonContext.Default.StartRunResponseJson);

        Assert.Equal("4", response.Refusal);
    }

    private static void AssertRoundTrip<T>(string fixture, JsonTypeInfo<T> typeInfo) where T : struct
    {
        var value = JsonSerializer.Deserialize(fixture, typeInfo);
        var written = JsonSerializer.Serialize(value, typeInfo);
        using var expected = JsonDocument.Parse(fixture);
        using var actual = JsonDocument.Parse(written);
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual.RootElement), written);
    }
}
