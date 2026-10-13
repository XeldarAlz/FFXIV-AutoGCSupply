using System.Text.Json.Serialization;

namespace AutoGcSupply.Core.Shopper.Model;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(QuoteStartRequestJson))]
[JsonSerializable(typeof(QuoteStartResponseJson))]
[JsonSerializable(typeof(QuoteResultResponseJson))]
[JsonSerializable(typeof(StartRunRequestJson))]
[JsonSerializable(typeof(StartRunResponseJson))]
[JsonSerializable(typeof(RunStatusResponseJson))]
internal sealed partial class ShopperJsonContext : JsonSerializerContext
{
}
