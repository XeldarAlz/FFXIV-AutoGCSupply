using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoGcSupply.Core.Shopper.Model;

internal sealed class ShopperNameConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString()!,
            JsonTokenType.Number => Encoding.UTF8.GetString(reader.ValueSpan),
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
