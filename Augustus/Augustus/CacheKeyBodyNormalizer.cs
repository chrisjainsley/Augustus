using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus;

internal static class CacheKeyBodyNormalizer
{
    private const string NormalizedPlaceholder = "__NORMALIZED__";

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
            return JsonSerializer.SerializeToUtf8Bytes(node);
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static void NormalizeNode(JsonNode node, IReadOnlyCollection<string> propertyNames)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in propertyNames)
                {
                    if (obj.ContainsKey(name))
                    {
                        obj[name] = NormalizedPlaceholder;
                    }
                }

                // Safe to iterate directly — the mutation loop above only replaces
                // values of matched keys; this loop only recurses into children.
                foreach (var kvp in obj)
                {
                    if (kvp.Value is JsonObject or JsonArray)
                    {
                        NormalizeNode(kvp.Value, propertyNames);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item is JsonObject or JsonArray)
                    {
                        NormalizeNode(item, propertyNames);
                    }
                }
                break;
        }
    }
}
