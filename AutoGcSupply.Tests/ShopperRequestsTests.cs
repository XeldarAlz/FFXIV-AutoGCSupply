using System.Text.Json;
using AutoGcSupply.Core.Shopper.Model;

namespace AutoGcSupply.Tests;

public sealed class ShopperRequestsTests
{
    [Fact]
    public void QuoteStartFromLinesMatchesTheContractExample()
    {
        var json = ShopperRequests.BuildQuoteStart([new ShopperQuoteLine(5057, 3)]);

        Assert.True(ShopperJsonEdit.SameJson(ShopperFixtures.QuoteStartRequest, json), json);
    }

    [Fact]
    public void QuoteStartKeepsTheOrderOfItsLines()
    {
        var json = ShopperRequests.BuildQuoteStart([new ShopperQuoteLine(5058, 1), new ShopperQuoteLine(5057, 12)]);

        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(5058u, items[0].GetProperty("itemId").GetUInt32());
        Assert.Equal(1, items[0].GetProperty("quantity").GetInt32());
        Assert.Equal(5057u, items[1].GetProperty("itemId").GetUInt32());
        Assert.Equal(12, items[1].GetProperty("quantity").GetInt32());
    }

    [Fact]
    public void StartRunFromLinesMatchesTheContractExample()
    {
        var json = ShopperRequests.BuildStartRun("0d7f...", "GC supply 2026-10-13", 150000, true, [new ShopperLine(5057, ShopperQuality.NormalQuality, 3, 700, 3)]);

        Assert.True(ShopperJsonEdit.SameJson(ShopperFixtures.StartRunRequest, json), json);
    }

    [Fact]
    public void StartRunWritesEveryQualityByItsName()
    {
        var json = ShopperRequests.BuildStartRun(
            "run",
            "label",
            1000,
            false,
            [
                new ShopperLine(1, ShopperQuality.Any, 1, 0, 0),
                new ShopperLine(2, ShopperQuality.HighQuality, 1, 0, 0),
                new ShopperLine(3, ShopperQuality.NormalQuality, 1, 0, 0),
            ]);

        using var document = JsonDocument.Parse(json);
        var lines = document.RootElement.GetProperty("lines");
        Assert.Equal("Any", lines[0].GetProperty("quality").GetString());
        Assert.Equal("HighQuality", lines[1].GetProperty("quality").GetString());
        Assert.Equal("NormalQuality", lines[2].GetProperty("quality").GetString());
    }

    [Fact]
    public void StartRunCarriesTheCapsExcessAndRunSettingsAsGiven()
    {
        var json = ShopperRequests.BuildStartRun(
            "4e1a0b3c-2d5f-4a6b-8c7d-9e0f1a2b3c4d",
            "GC supply",
            999_999_999,
            false,
            [
                new ShopperLine(5057, ShopperQuality.HighQuality, 2, 0, 0),
                new ShopperLine(5058, ShopperQuality.NormalQuality, 40, 1_250, 40),
            ]);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(ShopperContract.Schema, root.GetProperty("schema").GetInt32());
        Assert.Equal(ShopperContract.Caller, root.GetProperty("caller").GetString());
        Assert.Equal(ShopperContract.CallerDisplayName, root.GetProperty("callerDisplayName").GetString());
        Assert.Equal("4e1a0b3c-2d5f-4a6b-8c7d-9e0f1a2b3c4d", root.GetProperty("callerRunId").GetString());
        Assert.Equal("GC supply", root.GetProperty("label").GetString());
        Assert.Equal(999_999_999, root.GetProperty("maxSpend").GetInt64());
        Assert.False(root.GetProperty("returnHome").GetBoolean());
        var lines = root.GetProperty("lines");
        Assert.Equal(0, lines[0].GetProperty("maxUnitPrice").GetInt64());
        Assert.Equal(0, lines[0].GetProperty("excessAllowance").GetInt32());
        Assert.Equal(2, lines[0].GetProperty("quantity").GetInt32());
        Assert.Equal(1_250, lines[1].GetProperty("maxUnitPrice").GetInt64());
        Assert.Equal(40, lines[1].GetProperty("excessAllowance").GetInt32());
        Assert.Equal(40, lines[1].GetProperty("quantity").GetInt32());
    }
}
