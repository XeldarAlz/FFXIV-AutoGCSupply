using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoGcSupply.Tests;

internal static class ShopperJsonEdit
{
    public static string Edit(string json, Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        edit(root);
        return root.ToJsonString();
    }

    public static bool SameJson(string expected, string actual)
    {
        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        return JsonElement.DeepEquals(expectedDocument.RootElement, actualDocument.RootElement);
    }
}
