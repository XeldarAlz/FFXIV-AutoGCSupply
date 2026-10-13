using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AutoGcSupply.Core.Shopper.Model;

internal static class ShopperReplies
{
    public static ShopperAcceptance ParseQuoteStart(string? json)
    {
        if (json is null)
        {
            return ShopperAcceptance.Failed(ShopperReply.Unavailable);
        }

        if (!TryDeserialize(json, ShopperJsonContext.Default.QuoteStartResponseJson, out var reply))
        {
            return ShopperAcceptance.Failed(ShopperReply.Malformed);
        }

        return ToAcceptance(reply.Schema, reply.Accepted, reply.QuoteId, reply.Refusal, reply.Message);
    }

    public static ShopperAcceptance ParseStartRun(string? json)
    {
        if (json is null)
        {
            return ShopperAcceptance.Failed(ShopperReply.Unavailable);
        }

        if (!TryDeserialize(json, ShopperJsonContext.Default.StartRunResponseJson, out var reply))
        {
            return ShopperAcceptance.Failed(ShopperReply.Malformed);
        }

        return ToAcceptance(reply.Schema, reply.Accepted, reply.RunId, reply.Refusal, reply.Message);
    }

    public static ShopperQuoteView ParseQuoteResult(string? json)
    {
        if (json is null)
        {
            return ShopperQuoteView.Failed(ShopperReply.Unavailable);
        }

        if (!TryDeserialize(json, ShopperJsonContext.Default.QuoteResultResponseJson, out var reply))
        {
            return ShopperQuoteView.Failed(ShopperReply.Malformed);
        }

        var schema = CheckSchema(reply.Schema);
        if (schema != ShopperReply.Valid)
        {
            return ShopperQuoteView.Failed(schema);
        }

        if (!ShopperNames<ShopperQuoteState>.TryParse(reply.State, out var state))
        {
            return ShopperQuoteView.Failed(ShopperReply.Malformed);
        }

        var quoteId = reply.QuoteId ?? string.Empty;
        var refusal = ShopperNames<ShopperRefusal>.ParseOptional(reply.Refusal, ShopperRefusal.None, ShopperRefusal.Unknown);
        var refusalName = reply.Refusal ?? string.Empty;
        if (state != ShopperQuoteState.Ready)
        {
            return new ShopperQuoteView(ShopperReply.Valid, quoteId, state, refusal, refusalName, default, ImmutableArray<ShopperQuoteItem>.Empty);
        }

        if (reply.FetchedAtUtc is not { } fetchedAtUtc || reply.Items is not { } source || !TryReadQuoteItems(source, out var items))
        {
            return ShopperQuoteView.Failed(ShopperReply.Malformed);
        }

        return new ShopperQuoteView(ShopperReply.Valid, quoteId, state, refusal, refusalName, fetchedAtUtc, items);
    }

    public static ShopperRunView ParseRunStatus(string? json)
    {
        if (json is null)
        {
            return ShopperRunView.Failed(ShopperReply.Unavailable);
        }

        if (!TryDeserialize(json, ShopperJsonContext.Default.RunStatusResponseJson, out var reply))
        {
            return ShopperRunView.Failed(ShopperReply.Malformed);
        }

        return ToRunView(reply);
    }

    public static ShopperRunView ParseRunStatus(ReadOnlySpan<byte> utf8Json)
    {
        if (!TryDeserialize(utf8Json, ShopperJsonContext.Default.RunStatusResponseJson, out var reply))
        {
            return ShopperRunView.Failed(ShopperReply.Malformed);
        }

        return ToRunView(reply);
    }

    private static ShopperAcceptance ToAcceptance(int? schemaValue, bool? acceptedValue, string? id, string? refusalName, string? message)
    {
        var schema = CheckSchema(schemaValue);
        if (schema != ShopperReply.Valid)
        {
            return ShopperAcceptance.Failed(schema);
        }

        if (acceptedValue is not { } accepted)
        {
            return ShopperAcceptance.Failed(ShopperReply.Malformed);
        }

        if (accepted)
        {
            if (string.IsNullOrEmpty(id))
            {
                return ShopperAcceptance.Failed(ShopperReply.Malformed);
            }

            return new ShopperAcceptance(ShopperReply.Valid, id, ShopperRefusal.None, string.Empty, message ?? string.Empty);
        }

        var refusal = ShopperNames<ShopperRefusal>.ParseOr(refusalName, ShopperRefusal.Unknown);
        if (refusal == ShopperRefusal.None)
        {
            refusal = ShopperRefusal.Unknown;
        }

        return new ShopperAcceptance(ShopperReply.Valid, string.Empty, refusal, refusalName ?? string.Empty, message ?? string.Empty);
    }

    private static ShopperRunView ToRunView(in RunStatusResponseJson reply)
    {
        var schema = CheckSchema(reply.Schema);
        if (schema != ShopperReply.Valid)
        {
            return ShopperRunView.Failed(schema);
        }

        if (reply.Known is not { } known)
        {
            return ShopperRunView.Failed(ShopperReply.Malformed);
        }

        if (!known)
        {
            return ShopperRunView.UnknownRun(reply.RunId ?? string.Empty);
        }

        if (reply is not
            {
                DryRun: bool dryRun,
                Revision: long revision,
                Paused: bool paused,
                Terminal: bool terminal,
                NeedsPlayer: bool needsPlayer,
                ConfirmedSpend: long confirmedSpend and >= 0,
                ReservedSpend: long reservedSpend and >= 0,
                Lines: { } lineSource,
            })
        {
            return ShopperRunView.Failed(ShopperReply.Malformed);
        }

        var outcome = ShopperNames<ShopperRunOutcome>.ParseOptional(reply.Outcome, ShopperRunOutcome.None, ShopperRunOutcome.Unknown);
        if ((terminal && outcome == ShopperRunOutcome.None) || !TryReadRunLines(lineSource, out var lines))
        {
            return ShopperRunView.Failed(ShopperReply.Malformed);
        }

        return new ShopperRunView
        {
            Reply = ShopperReply.Valid,
            RunId = reply.RunId ?? string.Empty,
            Known = true,
            CallerRunId = reply.CallerRunId ?? string.Empty,
            DryRun = dryRun,
            Phase = ShopperNames<ShopperRunPhase>.ParseOr(reply.Phase, ShopperRunPhase.Unknown),
            State = ShopperNames<ShopperRunState>.ParseOr(reply.State, ShopperRunState.Unknown),
            Status = reply.Status ?? string.Empty,
            Revision = revision,
            Paused = paused,
            PauseReason = ShopperNames<ShopperPauseReason>.ParseOptional(reply.PauseReason, ShopperPauseReason.None, ShopperPauseReason.Unknown),
            Terminal = terminal,
            Outcome = outcome,
            NeedsPlayer = needsPlayer,
            ConfirmedSpend = confirmedSpend,
            ReservedSpend = reservedSpend,
            Lines = lines,
            Issues = ReadRunIssues(reply.Issues),
            ReturnHome = ShopperNames<ShopperReturnHome>.ParseOr(reply.ReturnHome, ShopperReturnHome.Unknown),
            Refusal = ShopperNames<ShopperRefusal>.ParseOptional(reply.Refusal, ShopperRefusal.None, ShopperRefusal.Unknown),
            RefusalName = reply.Refusal ?? string.Empty,
        };
    }

    private static bool TryReadQuoteItems(QuoteResultItemJson[] source, out ImmutableArray<ShopperQuoteItem> items)
    {
        var parsed = new ShopperQuoteItem[source.Length];
        for (var index = 0; index < source.Length; index++)
        {
            var item = source[index];
            if (item is not { ItemId: uint itemId, Quantity: int quantity and >= 0 } || !TryReadPrice(item.Normal, out var normal) || !TryReadPrice(item.High, out var high))
            {
                items = default;
                return false;
            }

            parsed[index] = new ShopperQuoteItem(itemId, quantity, normal, high);
        }

        items = ImmutableCollectionsMarshal.AsImmutableArray(parsed);
        return true;
    }

    private static bool TryReadPrice(QuotePriceJson? source, out ShopperQuotePrice price)
    {
        if (source is not { Available: bool available, Units: int units and >= 0, Total: long total and >= 0, UnitPrice: long unitPrice and >= 0, HighestUnitPrice: var highestUnitPrice and (null or >= 0) })
        {
            price = default;
            return false;
        }

        price = new ShopperQuotePrice(available, units, total, unitPrice, highestUnitPrice ?? 0);
        return true;
    }

    private static bool TryReadRunLines(RunStatusLineJson[] source, out ImmutableArray<ShopperRunLine> lines)
    {
        var parsed = new ShopperRunLine[source.Length];
        for (var index = 0; index < source.Length; index++)
        {
            var line = source[index];
            if (line is not { ItemId: uint itemId, Requested: int requested and >= 0, Acquired: int acquired and >= 0, Missing: int missing and >= 0 }
                || !TryReadQuality(line.Quality, out var quality))
            {
                lines = default;
                return false;
            }

            parsed[index] = new ShopperRunLine(itemId, quality, requested, acquired, missing);
        }

        lines = ImmutableCollectionsMarshal.AsImmutableArray(parsed);
        return true;
    }

    private static bool TryReadQuality(string? name, out ShopperQuality quality)
    {
        if (name is null)
        {
            quality = ShopperQuality.Any;
            return true;
        }

        return ShopperNames<ShopperQuality>.TryParse(name, out quality);
    }

    private static ImmutableArray<ShopperRunIssue> ReadRunIssues(RunStatusIssueJson[]? source)
    {
        if (source is null)
        {
            return ImmutableArray<ShopperRunIssue>.Empty;
        }

        var parsed = new ShopperRunIssue[source.Length];
        for (var index = 0; index < source.Length; index++)
        {
            var issue = source[index];
            parsed[index] = new ShopperRunIssue(ShopperNames<ShopperIssueReason>.ParseOr(issue.Reason, ShopperIssueReason.Unknown), issue.ItemId ?? 0);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(parsed);
    }

    private static ShopperReply CheckSchema(int? schema)
    {
        return schema switch
        {
            null => ShopperReply.Malformed,
            ShopperContract.Schema => ShopperReply.Valid,
            _ => ShopperReply.SchemaMismatch,
        };
    }

    private static bool TryDeserialize<T>(string json, JsonTypeInfo<T> typeInfo, out T value) where T : struct
    {
        try
        {
            value = JsonSerializer.Deserialize(json, typeInfo);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }

    private static bool TryDeserialize<T>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<T> typeInfo, out T value) where T : struct
    {
        try
        {
            value = JsonSerializer.Deserialize(utf8Json, typeInfo);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}
