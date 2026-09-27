using OpenAI.Chat;

namespace Augustus.AI;

internal static class AIResponseFormatting
{
    public static ChatCompletionOptions CreateJsonObjectChatOptions(AIOptions options, AIModelTier tier)
    {
        var chatOptions = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = options.MaxOutputTokens
        };

        // Reasoning models reject a custom temperature, so it is only pinned for non-reasoning tiers.
        if (tier.ReasoningEffort is null)
            chatOptions.Temperature = 0f;
        else
        {
            // Reasoning effort is marked experimental in the OpenAI SDK; the wire parameter itself is stable.
#pragma warning disable OPENAI001
            chatOptions.ReasoningEffortLevel = new ChatReasoningEffortLevel(tier.ReasoningEffort);
#pragma warning restore OPENAI001
        }

        return chatOptions;
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
