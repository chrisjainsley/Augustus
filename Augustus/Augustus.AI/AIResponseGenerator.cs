using System.Text.Json;
using OpenAI.Chat;

namespace Augustus.AI;

/// <summary>
/// Generates a response on a cache miss: routes to a starting tier, calls the model, validates the body,
/// and regenerates one tier higher with the rejection reason until it passes or the retries run out.
/// </summary>
internal sealed class AIResponseGenerator
{
    private readonly AIOptions options;
    private readonly IReadOnlyList<AIModelTier> tiers;
    private readonly OpenAIRequestHandler[] handlers;

    public AIResponseGenerator(AIOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        tiers = options.ResolveModelTiers();
        handlers = tiers
            .Select(tier => new OpenAIRequestHandler(OpenAIClientFactory.Create(options, tier), options, tier))
            .ToArray();
    }

    /// <param name="dedupeKey">The request's cache hash; concurrent identical requests share each model call.</param>
    public async Task<AIGenerationResult> GenerateAsync(
        string dedupeKey,
        AIGenerationContext context,
        CancellationToken cancellationToken)
    {
        var start = await SelectStartTierAsync(context, cancellationToken).ConfigureAwait(false);
        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(string.Join("\n\n", context.Instructions)),
            ChatMessage.CreateUserMessage(context.SanitizedCurlRequest)
        };

        string failure = string.Empty;
        var attempts = 0;
        for (var attempt = 0; attempt <= options.MaxValidationRetries; attempt++)
        {
            var tierIndex = Math.Min(start + attempt, tiers.Count - 1);
            var tier = tiers[tierIndex];
            attempts++;

            var result = await handlers[tierIndex]
                .CompleteChatWithRetryAsync(
                    $"{dedupeKey}|{attempt}|{tierIndex}",
                    messages,
                    AIResponseFormatting.CreateJsonObjectChatOptions(options, tier),
                    cancellationToken)
                .ConfigureAwait(false);

            var text = result?.Value?.Content is { Count: > 0 } content ? content[0]?.Text : null;
            var body = string.IsNullOrEmpty(text) ? string.Empty : AIResponseFormatting.StripMarkdownFences(text!);
            body = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(body, context.Path);

            var validation = await ValidateAsync(context, body, cancellationToken).ConfigureAwait(false);
            if (validation.Failure is null)
                return AIGenerationResult.Succeeded(body, tier.Model, attempts);
            failure = validation.Failure;
            if (validation.IsFatal)
                break;

            messages.Add(ChatMessage.CreateAssistantMessage(body.Length == 0 ? "(empty response)" : body));
            messages.Add(ChatMessage.CreateUserMessage(
                $"That response was rejected: {failure}. Return the corrected JSON response only."));
        }

        return AIGenerationResult.Failed(failure, attempts);
    }

    private async Task<int> SelectStartTierAsync(AIGenerationContext context, CancellationToken cancellationToken)
    {
        if (options.ModelRouter is null || tiers.Count < 2)
            return 0;

        try
        {
            var selected = await options.ModelRouter.SelectTierAsync(context, tiers, cancellationToken).ConfigureAwait(false);
            return Math.Clamp(selected, 0, tiers.Count - 1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // A router is an optimisation: when it fails, start at the first tier and let validation escalate.
            Console.WriteLine($"[AI Router] {options.ModelRouter.GetType().Name} failed, starting at the first tier: {ex.Message}");
            return 0;
        }
    }

    /// <returns>A null failure when the body passes. A fatal failure is not worth regenerating for.</returns>
    private async Task<(string? Failure, bool IsFatal)> ValidateAsync(AIGenerationContext context, string body, CancellationToken cancellationToken)
    {
        if (body.Length == 0)
            return ("response was empty", false);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return ("response is not valid JSON", false);
        }

        using (document)
        {
            foreach (var validator in options.ResponseValidators)
            {
                var name = validator.GetType().Name;
                AIResponseValidationResult result;
                try
                {
                    result = await validator
                        .ValidateAsync(context, document.RootElement, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    // A broken validator fails the same way for every body, so regenerating would only burn model calls.
                    return ($"{name} threw {ex.GetType().Name}: {ex.Message}", true);
                }

                if (!result.IsValid)
                    return ($"{name}: {result.Reason ?? "rejected"}", false);
            }
        }

        return (null, false);
    }
}

/// <summary>The outcome of <see cref="AIResponseGenerator.GenerateAsync"/>.</summary>
internal sealed record AIGenerationResult(string? Body, string? Model, string? Failure, int Attempts)
{
    public bool IsSuccess => Body is not null;

    public static AIGenerationResult Succeeded(string body, string model, int attempts) => new(body, model, null, attempts);

    public static AIGenerationResult Failed(string failure, int attempts) => new(null, null, failure, attempts);
}
