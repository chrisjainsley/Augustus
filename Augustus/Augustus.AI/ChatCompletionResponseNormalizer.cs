using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus.AI;

/// <summary>
/// Normalizes AI-generated chat completion response JSON to fix type mismatches
/// that cause OpenAI SDK deserialization failures.
/// </summary>
/// <remarks>
/// The AI model sometimes generates fields with incorrect JSON types (e.g., a string
/// where the SDK expects an object). This normalizer fixes known type mismatches so
/// responses are compatible with OpenAI SDK 2.9.1+ strict deserialization.
/// </remarks>
internal static class ChatCompletionResponseNormalizer
{
    /// <summary>
    /// Normalizes the JSON response if the request path is a chat completion endpoint.
    /// Returns the original JSON unchanged for non-chat-completion paths or if parsing fails.
    /// </summary>
    public static string NormalizeIfChatCompletion(string jsonResponse, string requestPath)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse) || string.IsNullOrWhiteSpace(requestPath))
            return jsonResponse;

        if (!IsChatCompletionPath(requestPath))
            return jsonResponse;

        try
        {
            var node = JsonNode.Parse(jsonResponse);
            if (node is not JsonObject root)
                return jsonResponse;

            NormalizeTopLevel(root);
            NormalizeChoices(root);
            NormalizeUsage(root);

            return root.ToJsonString();
        }
        catch (JsonException)
        {
            return jsonResponse;
        }
    }

    internal static bool IsChatCompletionPath(string path)
    {
        return path.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase);
    }

    private static void NormalizeTopLevel(JsonObject root)
    {
        EnsureString(root, "id", () => $"chatcmpl-{Guid.NewGuid():N}"[..20]);
        EnsureString(root, "object", () => "chat.completion");
        EnsureString(root, "model", () => "gpt-4");
        EnsureNumber(root, "created", () => DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        if (!root.TryGetPropertyValue("system_fingerprint", out var sfNode))
        {
            root["system_fingerprint"] = $"fp_{Guid.NewGuid():N}"[..14];
        }
        else
        {
            EnsureStringOrNull(root, "system_fingerprint");
        }
    }

    private static void NormalizeChoices(JsonObject root)
    {
        if (!root.TryGetPropertyValue("choices", out var choicesNode))
        {
            root["choices"] = new JsonArray();
            return;
        }

        choicesNode = TryRehydrateStringifiedNode(root, "choices", choicesNode, () => new JsonArray());
        if (choicesNode is null)
            return;

        if (choicesNode is not JsonArray choicesArray)
        {
            root["choices"] = new JsonArray();
            return;
        }

        for (var i = 0; i < choicesArray.Count; i++)
        {
            var choiceItem = choicesArray[i];

            if (choiceItem is JsonValue cv && cv.GetValueKind() == JsonValueKind.String)
            {
                var str = cv.GetValue<string>();
                try
                {
                    var parsed = JsonNode.Parse(str);
                    if (parsed is JsonObject parsedChoice)
                    {
                        choicesArray[i] = parsedChoice;
                        choiceItem = parsedChoice;
                    }
                    else
                    {
                        choicesArray[i] = CreateDefaultChoice();
                        choiceItem = choicesArray[i];
                    }
                }
                catch (JsonException)
                {
                    choicesArray[i] = CreateDefaultChoice();
                    choiceItem = choicesArray[i];
                }
            }

            if (choiceItem is not JsonObject choice)
                continue;

            NormalizeChoice(choice);
        }
    }

    private static JsonObject CreateDefaultChoice()
    {
        return new JsonObject
        {
            ["index"] = 0,
            ["message"] = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = (string?)null
            },
            ["finish_reason"] = "stop"
        };
    }

    private static void NormalizeChoice(JsonObject choice)
    {
        EnsureNumber(choice, "index", () => 0);
        EnsureString(choice, "finish_reason", () => "stop");

        if (choice.TryGetPropertyValue("logprobs", out var logprobs)
            && logprobs is JsonValue lv && lv.GetValueKind() == JsonValueKind.String)
        {
            choice["logprobs"] = null;
        }

        NormalizeMessage(choice);
        NormalizeContentFilterResults(choice);
    }

    private static void NormalizeMessage(JsonObject choice)
    {
        if (!choice.TryGetPropertyValue("message", out var messageNode))
        {
            choice["message"] = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = (string?)null
            };
            return;
        }

        if (messageNode is JsonValue mv && mv.GetValueKind() == JsonValueKind.String)
        {
            var str = mv.GetValue<string>();

            try
            {
                var parsed = JsonNode.Parse(str);
                if (parsed is JsonObject parsedObj)
                {
                    choice["message"] = parsedObj;
                    NormalizeMessageFields(parsedObj);
                    return;
                }
            }
            catch (JsonException) { }

            choice["message"] = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = str
            };
            return;
        }

        if (messageNode is not JsonObject message)
            return;

        NormalizeMessageFields(message);
    }

    private static void NormalizeMessageFields(JsonObject message)
    {
        EnsureString(message, "role", () => "assistant");

        if (message.TryGetPropertyValue("content", out var contentNode))
        {
            if (contentNode is JsonObject || contentNode is JsonArray)
            {
                message["content"] = contentNode.ToJsonString();
            }
            else if (contentNode is JsonValue cv
                     && cv.GetValueKind() != JsonValueKind.String
                     && cv.GetValueKind() != JsonValueKind.Null)
            {
                message["content"] = cv.ToString();
            }
        }

        NormalizeToolCalls(message);
    }

    private static void NormalizeToolCalls(JsonObject message)
    {
        if (!message.TryGetPropertyValue("tool_calls", out var toolCallsNode))
            return;

        toolCallsNode = TryRehydrateStringifiedNode(message, "tool_calls", toolCallsNode, () => new JsonArray());
        if (toolCallsNode is null)
            return;

        if (toolCallsNode is not JsonArray toolCalls)
        {
            message["tool_calls"] = new JsonArray();
            return;
        }

        foreach (var tcItem in toolCalls)
        {
            if (tcItem is not JsonObject toolCall)
                continue;

            EnsureString(toolCall, "id", () => $"call_{Guid.NewGuid():N}"[..17]);
            EnsureString(toolCall, "type", () => "function");

            NormalizeToolCallFunction(toolCall);
        }
    }

    private static void NormalizeToolCallFunction(JsonObject toolCall)
    {
        if (!toolCall.TryGetPropertyValue("function", out var funcNode))
            return;

        funcNode = TryRehydrateStringifiedNode(toolCall, "function", funcNode, () => new JsonObject { ["name"] = "unknown", ["arguments"] = "{}" });
        if (funcNode is null or JsonValue)
        {
            toolCall["function"] = new JsonObject { ["name"] = "unknown", ["arguments"] = "{}" };
            return;
        }

        if (funcNode is not JsonObject function)
        {
            toolCall["function"] = new JsonObject { ["name"] = "unknown", ["arguments"] = "{}" };
            return;
        }

        EnsureString(function, "name", () => "unknown");

        if (function.TryGetPropertyValue("arguments", out var argsNode))
        {
            if (argsNode is JsonObject || argsNode is JsonArray)
            {
                function["arguments"] = argsNode.ToJsonString();
            }
            else if (argsNode is JsonValue av
                     && av.GetValueKind() != JsonValueKind.String
                     && av.GetValueKind() != JsonValueKind.Null)
            {
                function["arguments"] = av.ToString();
            }
        }
    }

    private static void NormalizeContentFilterResults(JsonObject choice)
    {
        if (choice.TryGetPropertyValue("content_filter_results", out var filterNode)
            && filterNode is JsonValue fv && fv.GetValueKind() == JsonValueKind.String)
        {
            choice["content_filter_results"] = CreateDefaultContentFilterResults();
        }
    }

    private static JsonObject CreateDefaultContentFilterResults()
    {
        return new JsonObject
        {
            ["hate"] = new JsonObject { ["filtered"] = false, ["severity"] = "safe" },
            ["self_harm"] = new JsonObject { ["filtered"] = false, ["severity"] = "safe" },
            ["sexual"] = new JsonObject { ["filtered"] = false, ["severity"] = "safe" },
            ["violence"] = new JsonObject { ["filtered"] = false, ["severity"] = "safe" }
        };
    }

    private static void NormalizeUsage(JsonObject root)
    {
        if (!root.TryGetPropertyValue("usage", out var usageNode))
        {
            root["usage"] = CreateDefaultUsage();
            return;
        }

        usageNode = TryRehydrateStringifiedNode(root, "usage", usageNode, CreateDefaultUsage);
        if (usageNode is null)
            return;

        if (usageNode is not JsonObject usage)
        {
            root["usage"] = CreateDefaultUsage();
            return;
        }

        EnsureNumber(usage, "prompt_tokens", () => 0);
        EnsureNumber(usage, "completion_tokens", () => 0);
        EnsureNumber(usage, "total_tokens", () => 0);
    }

    private static JsonObject CreateDefaultUsage()
    {
        return new JsonObject
        {
            ["prompt_tokens"] = 0,
            ["completion_tokens"] = 0,
            ["total_tokens"] = 0
        };
    }

    private static JsonNode? TryRehydrateStringifiedNode(JsonObject parent, string key, JsonNode? node, Func<JsonNode> defaultFactory)
    {
        if (node is not JsonValue jv || jv.GetValueKind() != JsonValueKind.String)
            return node;

        var str = jv.GetValue<string>();
        try
        {
            var parsed = JsonNode.Parse(str);
            if (parsed is null)
            {
                parent[key] = defaultFactory();
                return null;
            }
            parent[key] = parsed;
            return parsed;
        }
        catch (JsonException)
        {
            parent[key] = defaultFactory();
            return null;
        }
    }

    private static void EnsureString(JsonObject obj, string key, Func<string> defaultValue)
    {
        if (!obj.TryGetPropertyValue(key, out var node))
        {
            obj[key] = defaultValue();
            return;
        }

        if (node is JsonValue v && v.GetValueKind() != JsonValueKind.String)
        {
            obj[key] = v.GetValueKind() switch
            {
                JsonValueKind.Number => v.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => defaultValue()
            };
        }
    }

    private static void EnsureStringOrNull(JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var node))
            return;

        if (node is null || (node is JsonValue v && v.GetValueKind() == JsonValueKind.Null))
            return;

        if (node is JsonValue val && val.GetValueKind() != JsonValueKind.String)
        {
            obj[key] = val.GetValueKind() switch
            {
                JsonValueKind.Number => val.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => (string?)null
            };
        }
    }

    private static void EnsureNumber(JsonObject obj, string key, Func<long> defaultValue)
    {
        if (!obj.TryGetPropertyValue(key, out var node))
        {
            obj[key] = defaultValue();
            return;
        }

        if (node is JsonValue v && v.GetValueKind() == JsonValueKind.String)
        {
            var str = v.GetValue<string>();
            obj[key] = long.TryParse(str, out var num) ? num : defaultValue();
        }
    }
}
