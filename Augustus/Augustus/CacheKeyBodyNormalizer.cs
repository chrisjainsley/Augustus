using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus;

internal static class CacheKeyBodyNormalizer
{
    private const string NormalizedPlaceholder = "__NORMALIZED__";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static byte[] NormalizeForCacheKey(byte[] body, IReadOnlyCollection<string> propertyNames)
    {
        if (propertyNames.Count == 0 || body.Length == 0)
            return body;

        try
        {
            var node = JsonNode.Parse(body);
            if (node is null)
                return body;

            NormalizeNode(node, propertyNames);
            return JsonSerializer.SerializeToUtf8Bytes(node, SerializerOptions);
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static bool NormalizeNode(JsonNode node, IReadOnlyCollection<string> propertyNames)
    {
        var changed = false;
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in propertyNames)
                {
                    if (name is not null && obj.ContainsKey(name))
                    {
                        obj[name] = NormalizedPlaceholder;
                        changed = true;
                    }
                }

                foreach (var kvp in obj)
                {
                    if (kvp.Value is JsonObject or JsonArray)
                    {
                        changed |= NormalizeNode(kvp.Value, propertyNames);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item is JsonObject or JsonArray)
                    {
                        changed |= NormalizeNode(item, propertyNames);
                    }
                }
                break;
        }
        return changed;
    }
}
