using System.Text;
using System.Text.Json;

namespace AutoGcSupply.Core.Shopper.Model;

internal sealed class ShopperRunStatusCache
{
    private const int InitialBufferBytes = 4096;

    private byte[] buffer = new byte[InitialBufferBytes];
    private string? cachedRunId;
    private RunStamp cachedStamp;
    private ShopperRunView cachedView;

    public ShopperRunView Read(string runId, string? json)
    {
        if (json is null)
        {
            return ShopperRunView.Failed(ShopperReply.Unavailable);
        }

        var utf8Json = Encode(json);
        var hasStamp = TryReadStamp(utf8Json, out var stamp);
        if (hasStamp && stamp == cachedStamp && string.Equals(runId, cachedRunId, StringComparison.Ordinal))
        {
            return cachedView;
        }

        var view = ShopperReplies.ParseRunStatus(utf8Json);
        cachedRunId = hasStamp && view.Reply == ShopperReply.Valid ? runId : null;
        cachedStamp = stamp;
        cachedView = view;
        return view;
    }

    private ReadOnlySpan<byte> Encode(string json)
    {
        var byteCount = Encoding.UTF8.GetByteCount(json);
        if (byteCount > buffer.Length)
        {
            buffer = new byte[Math.Max(byteCount, buffer.Length * 2)];
        }

        var written = Encoding.UTF8.GetBytes(json, buffer);
        return buffer.AsSpan(0, written);
    }

    private static bool TryReadStamp(ReadOnlySpan<byte> utf8Json, out RunStamp stamp)
    {
        stamp = default;
        int? schema = null;
        bool? known = null;
        long? revision = null;
        var terminal = false;
        var paused = false;
        var needsPlayer = false;
        var phase = 0;
        var status = 0;
        var pauseReason = 0;
        try
        {
            var reader = new Utf8JsonReader(utf8Json);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                var property = StampPropertyOf(ref reader);
                reader.Read();
                switch (property)
                {
                    case StampProperty.Schema:
                        schema = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var schemaValue) ? schemaValue : null;
                        break;
                    case StampProperty.Known:
                        known = FlagOf(reader.TokenType);
                        break;
                    case StampProperty.Revision:
                        revision = reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var revisionValue) ? revisionValue : null;
                        break;
                    case StampProperty.Terminal:
                        terminal = FlagOf(reader.TokenType) == true;
                        break;
                    case StampProperty.Paused:
                        paused = FlagOf(reader.TokenType) == true;
                        break;
                    case StampProperty.NeedsPlayer:
                        needsPlayer = FlagOf(reader.TokenType) == true;
                        break;
                    case StampProperty.Phase:
                        phase = ValueKeyOf(ref reader);
                        break;
                    case StampProperty.Status:
                        status = ValueKeyOf(ref reader);
                        break;
                    case StampProperty.PauseReason:
                        pauseReason = ValueKeyOf(ref reader);
                        break;
                }

                reader.Skip();
            }
        }
        catch (JsonException)
        {
            return false;
        }

        if (schema != ShopperContract.Schema || known is not { } isKnown || revision is not { } revisionNumber)
        {
            return false;
        }

        stamp = new RunStamp(isKnown, revisionNumber, terminal, paused, needsPlayer, phase, status, pauseReason);
        return true;
    }

    private static bool? FlagOf(JsonTokenType tokenType) => tokenType switch
    {
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        _ => null,
    };

    private static int ValueKeyOf(ref Utf8JsonReader reader)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.Number))
        {
            return 0;
        }

        var hash = new HashCode();
        hash.AddBytes(reader.ValueSpan);
        return hash.ToHashCode();
    }

    private static StampProperty StampPropertyOf(ref Utf8JsonReader reader)
    {
        if (reader.ValueIsEscaped)
        {
            return StampProperty.Other;
        }

        var name = reader.ValueSpan;
        if (Ascii.EqualsIgnoreCase(name, "revision"u8))
        {
            return StampProperty.Revision;
        }

        if (Ascii.EqualsIgnoreCase(name, "known"u8))
        {
            return StampProperty.Known;
        }

        if (Ascii.EqualsIgnoreCase(name, "terminal"u8))
        {
            return StampProperty.Terminal;
        }

        if (Ascii.EqualsIgnoreCase(name, "paused"u8))
        {
            return StampProperty.Paused;
        }

        if (Ascii.EqualsIgnoreCase(name, "needsPlayer"u8))
        {
            return StampProperty.NeedsPlayer;
        }

        if (Ascii.EqualsIgnoreCase(name, "phase"u8))
        {
            return StampProperty.Phase;
        }

        if (Ascii.EqualsIgnoreCase(name, "status"u8))
        {
            return StampProperty.Status;
        }

        if (Ascii.EqualsIgnoreCase(name, "pauseReason"u8))
        {
            return StampProperty.PauseReason;
        }

        return Ascii.EqualsIgnoreCase(name, "schema"u8) ? StampProperty.Schema : StampProperty.Other;
    }

    private enum StampProperty : byte
    {
        Other,
        Schema,
        Known,
        Revision,
        Terminal,
        Paused,
        NeedsPlayer,
        Phase,
        Status,
        PauseReason,
    }

    private readonly record struct RunStamp(bool Known, long Revision, bool Terminal, bool Paused, bool NeedsPlayer, int Phase, int Status, int PauseReason);
}
