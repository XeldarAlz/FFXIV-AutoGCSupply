using System.Text.Json;

namespace AutoGcSupply.Core.Shopper.Model;

internal static class ShopperRequests
{
    public static string BuildQuoteStart(ReadOnlySpan<ShopperQuoteLine> lines)
    {
        var items = new QuoteStartItemJson[lines.Length];
        for (var index = 0; index < lines.Length; index++)
        {
            items[index] = new QuoteStartItemJson { ItemId = lines[index].ItemId, Quantity = lines[index].Quantity };
        }

        var request = new QuoteStartRequestJson
        {
            Schema = ShopperContract.Schema,
            Caller = ShopperContract.Caller,
            CallerDisplayName = ShopperContract.CallerDisplayName,
            Items = items,
        };
        return JsonSerializer.Serialize(request, ShopperJsonContext.Default.QuoteStartRequestJson);
    }

    public static string BuildStartRun(string callerRunId, string label, long maxSpend, bool returnHome, ReadOnlySpan<ShopperLine> lines)
    {
        var requestLines = new StartRunLineJson[lines.Length];
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            requestLines[index] = new StartRunLineJson
            {
                ItemId = line.ItemId,
                Quality = line.Quality,
                Quantity = line.Quantity,
                MaxUnitPrice = line.MaxUnitPrice,
                ExcessAllowance = line.ExcessAllowance,
            };
        }

        var request = new StartRunRequestJson
        {
            Schema = ShopperContract.Schema,
            Caller = ShopperContract.Caller,
            CallerDisplayName = ShopperContract.CallerDisplayName,
            CallerRunId = callerRunId,
            Label = label,
            MaxSpend = maxSpend,
            ReturnHome = returnHome,
            Lines = requestLines,
        };
        return JsonSerializer.Serialize(request, ShopperJsonContext.Default.StartRunRequestJson);
    }
}
