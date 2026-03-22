using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus;

internal static class CacheKeyBodyNormalizer
{
    private const string NormalizedPlaceholder = "__NORMALIZED__";

    // Use consistent JSON serialization options to ensure platform-independent cache key generation.
    // WriteIndented=false ensures no line-ending differences between Windows (\r\n) and Linux (\n).
    // UnsafeRelaxedJsonEscaping passes through non-ASCII characters, HTML-sensitive characters
    // (<, >, &, ', +), and other symbols verbatim instead of escaping them to \uXXXX sequences.
    // This avoids platform-specific unicode escaping differences at the cost of producing JSON
    // that is not safe for direct embedding in HTML. This is acceptable here because the output
    // is only used for cache key computation, never rendered in a browser.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Produces the UTF-8 JSON bytes used when hashing the request body for cache keys.
    /// Valid JSON is parsed, object keys are sorted lexicographically at every depth (cross-platform
    /// semantic equivalence), optional dynamic fields are replaced with a placeholder, then the
    /// result is serialized with <see cref="SerializerOptions"/>. Non-JSON bodies are returned unchanged.
    /// </summary>
    public static byte[] PrepareBodyForCacheKey(byte[] body, IReadOnlyCollection<string>? propertyNames)
    {
        propertyNames ??= Array.Empty<string>();
        if (body.Length == 0)
            return body;

        if (!LooksLikeJson(body))
            return body;

        try
        {
            var node = JsonNode.Parse(body);
            if (node is null)
                return body;

            var canonical = SortKeysRecursive(node);
            if (canonical is null)
                return body;

            // Normalize \r\n → \n in all JSON string values so cache keys are identical
            // regardless of the platform that generated the request body. This is necessary
            // because StringBuilder.AppendLine(), TextWriter.WriteLine(), and similar APIs
            // use Environment.NewLine which is \r\n on Windows and \n on Linux.
            NormalizeNewlines(canonical);

            if (propertyNames.Count > 0)
                NormalizeNode(canonical, propertyNames);

            return JsonSerializer.SerializeToUtf8Bytes(canonical, SerializerOptions);
        }
        catch (JsonException)
        {
            return body;
        }
    }

    /// <summary>
    /// Backward-compatible name; delegates to <see cref="PrepareBodyForCacheKey"/>.
    /// </summary>
    public static byte[] NormalizeForCacheKey(byte[] body, IReadOnlyCollection<string> propertyNames)
        => PrepareBodyForCacheKey(body, propertyNames);

    /// <summary>
    /// Deep-clones a subtree into nodes with no parent (JsonNode cannot be re-parented).
    /// Uses round-trip for <see cref="JsonValue"/> leaves so we do not depend on JsonNode.DeepClone (net8+ only).
    /// </summary>
    private static JsonNode? DetachCopy(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonObject obj:
                var o = new JsonObject();
                foreach (var kvp in obj)
                    o[kvp.Key] = DetachCopy(kvp.Value);
                return o;
            case JsonArray arr:
                var a = new JsonArray();
                foreach (var item in arr)
                    a.Add(DetachCopy(item));
                return a;
            case JsonValue value when value.TryGetValue<JsonElement>(out var element):
                return JsonValue.Create(element.Clone());
            default:
                return JsonNode.Parse(node.ToJsonString())!;
        }
    }

    /// <summary>
    /// Builds a deep copy of the JSON tree with every <see cref="JsonObject"/> having properties
    /// ordered by key (<see cref="StringComparer.Ordinal"/>) so semantically equivalent payloads
    /// hash the same across clients that emit different property orders.
    /// </summary>
    private static JsonNode? SortKeysRecursive(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonObject obj:
                var sorted = new JsonObject();
                foreach (var kvp in obj.OrderBy(static kvp => kvp.Key, StringComparer.Ordinal))
                {
                    sorted[kvp.Key] = SortKeysRecursive(kvp.Value);
                }
                return sorted;
            case JsonArray arr:
                var newArr = new JsonArray();
                foreach (var item in arr)
                {
                    newArr.Add(SortKeysRecursive(item));
                }
                return newArr;
            default:
                return DetachCopy(node);
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
                        changed |= NormalizeNode(kvp.Value!, propertyNames);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item is JsonObject or JsonArray)
                    {
                        changed |= NormalizeNode(item!, propertyNames);
                    }
                }
                break;
        }
        return changed;
    }

    /// <summary>
    /// Replaces <c>\r\n</c> with <c>\n</c> in every JSON string value so that cache keys
    /// are identical regardless of the OS that produced the request body.
    /// </summary>
    private static void NormalizeNewlines(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    var child = obj[key];
                    if (child is JsonValue val && val.TryGetValue<string>(out var s) && s.Contains('\r'))
                        obj[key] = s.ReplaceLineEndings("\n");
                    else if (child is JsonObject or JsonArray)
                        NormalizeNewlines(child);
                }
                break;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    var child = arr[i];
                    if (child is JsonValue val && val.TryGetValue<string>(out var s) && s.Contains('\r'))
                        arr[i] = s.ReplaceLineEndings("\n");
                    else if (child is JsonObject or JsonArray)
                        NormalizeNewlines(child!);
                }
                break;
        }
    }

    private static bool LooksLikeJson(byte[] body)
    {
        foreach (var b in body)
        {
            switch (b)
            {
                case (byte)' ':
                case (byte)'\t':
                case (byte)'\r':
                case (byte)'\n':
                    continue;
                case (byte)'{':
                case (byte)'[':
                case (byte)'"':
                case >= (byte)'0' and <= (byte)'9':
                case (byte)'-':
                case (byte)'t':
                case (byte)'f':
                case (byte)'n':
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }
}
