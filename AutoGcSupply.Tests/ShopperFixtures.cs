namespace AutoGcSupply.Tests;

internal static class ShopperFixtures
{
    public const string QuoteStartRequest = """
        {
          "schema": 1,
          "caller": "AutoGcSupply",
          "callerDisplayName": "Auto GC Supply",
          "items": [
            { "itemId": 5057, "quantity": 3 }
          ]
        }
        """;

    public const string QuoteStartAccepted = """
        { "schema": 1, "accepted": true, "quoteId": "b6c0...", "refusal": null, "message": null }
        """;

    public const string QuoteStartRefused = """
        { "schema": 1, "accepted": false, "quoteId": null, "refusal": "InvalidRequest", "message": "localized text" }
        """;

    public const string QuoteResult = """
        {
          "schema": 1,
          "quoteId": "b6c0...",
          "state": "Ready",
          "refusal": null,
          "fetchedAtUtc": "2026-10-13T18:22:05Z",
          "items": [
            {
              "itemId": 5057,
              "quantity": 3,
              "normal": { "available": true, "units": 3, "total": 1890, "unitPrice": 630, "oldestListingHours": 4.5, "worlds": 1 },
              "high":   { "available": false, "units": 0, "total": 0, "unitPrice": 0, "oldestListingHours": 0, "worlds": 0 }
            }
          ]
        }
        """;

    public const string QuoteResultWithHighest = """
        {
          "schema": 1,
          "quoteId": "b6c0...",
          "state": "Ready",
          "refusal": null,
          "fetchedAtUtc": "2026-10-13T18:22:05Z",
          "items": [
            {
              "itemId": 5057,
              "quantity": 3,
              "normal": { "available": true, "units": 3, "total": 1890, "unitPrice": 630, "highestUnitPrice": 700, "oldestListingHours": 4.5, "worlds": 1 },
              "high":   { "available": false, "units": 0, "total": 0, "unitPrice": 0, "highestUnitPrice": 0, "oldestListingHours": 0, "worlds": 0 }
            }
          ]
        }
        """;

    public const string StartRunRequest = """
        {
          "schema": 1,
          "caller": "AutoGcSupply",
          "callerDisplayName": "Auto GC Supply",
          "callerRunId": "0d7f...",
          "label": "GC supply 2026-10-13",
          "maxSpend": 150000,
          "returnHome": true,
          "lines": [
            { "itemId": 5057, "quality": "NormalQuality", "quantity": 3, "maxUnitPrice": 700, "excessAllowance": 3 }
          ]
        }
        """;

    public const string StartRunAccepted = """
        { "schema": 1, "accepted": true, "runId": "4e1a...", "refusal": null, "message": null }
        """;

    public const string RunStatus = """
        {
          "schema": 1,
          "runId": "4e1a...",
          "known": true,
          "callerRunId": "0d7f...",
          "phase": "Shopping",
          "state": "Searching",
          "status": "Searching the listings of Darksteel Ore",
          "revision": 42,
          "paused": false,
          "pauseReason": null,
          "terminal": false,
          "outcome": null,
          "needsPlayer": false,
          "confirmedSpend": 1890,
          "reservedSpend": 0,
          "lines": [
            { "itemId": 5057, "quality": "NormalQuality", "requested": 3, "acquired": 3, "missing": 0 }
          ],
          "issues": [ { "reason": "NoStock", "itemId": 5058 } ],
          "returnHome": "NotNeeded",
          "refusal": null
        }
        """;
}
