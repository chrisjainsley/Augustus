using OpenAI.Chat;

namespace Augustus.AI;

internal static class AIResponseFormatting
{
    public static ChatCompletionOptions CreateJsonObjectChatOptions()
    {
        return new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };
    }

    public static string StripMarkdownFences(string text)
    {
        var trimmed = text.Trim();
        const string jsonFence = "```json";
        const string fence = "```";

        if (trimmed.StartsWith(jsonFence, StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[jsonFence.Length..];
        else if (trimmed.StartsWith(fence))
            trimmed = trimmed[fence.Length..];

        if (trimmed.EndsWith(fence))
            trimmed = trimmed[..^fence.Length];

        return trimmed.Trim();
    }
}
