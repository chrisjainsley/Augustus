using System.Text.Json;

namespace Augustus.AI;

/// <summary>
/// One rung of the model ladder used to generate a response. Generation starts at the tier an
/// <see cref="IAIModelRouter"/> selects (or the first tier) and moves one tier up each time a
/// response fails validation.
/// </summary>
/// <param name="Model">The model name, or the deployment name when the tier targets Azure OpenAI.</param>
/// <param name="Description">
/// When this tier is the right choice, in plain language. Routers use it to match a request to a tier.
/// </param>
public sealed record AIModelTier(string Model, string Description)
{
    /// <summary>
    /// An OpenAI-compatible endpoint for this tier (for example <c>https://api.groq.com/openai/v1</c>).
    /// When null, the tier uses OpenAI, or the Azure endpoint when <see cref="AIOptions.UseAzureOpenAI"/> is set.
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// The API key for <see cref="Endpoint"/>. When null, <see cref="AIOptions.OpenAIApiKey"/> is used.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// The reasoning effort sent with the request: <c>none</c>, <c>minimal</c>, <c>low</c>, <c>medium</c> or <c>high</c>.
    /// When null, no reasoning effort is sent, which non-reasoning models require.
    /// </summary>
    public string? ReasoningEffort { get; init; }
}

/// <summary>
/// The request a response is being generated for, as seen by routers and validators.
/// </summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The request path.</param>
/// <param name="SanitizedCurlRequest">The request as a cURL command with sensitive values removed.</param>
/// <param name="Instructions">The instructions that apply to this request.</param>
public sealed record AIGenerationContext(
    string Method,
    string Path,
    string SanitizedCurlRequest,
    IReadOnlyList<string> Instructions);

/// <summary>
/// Chooses which <see cref="AIModelTier"/> generates a response. Runs once per cache miss.
/// </summary>
public interface IAIModelRouter
{
    /// <summary>
    /// Returns the index into <paramref name="tiers"/> that generation should start from.
    /// </summary>
    ValueTask<int> SelectTierAsync(
        AIGenerationContext context,
        IReadOnlyList<AIModelTier> tiers,
        CancellationToken cancellationToken);
}

/// <summary>
/// Checks a freshly generated response before it is returned or cached. Runs only on a cache miss.
/// </summary>
public interface IAIResponseValidator
{
    /// <summary>
    /// Validates <paramref name="response"/> for the request described by <paramref name="context"/>.
    /// </summary>
    ValueTask<AIResponseValidationResult> ValidateAsync(
        AIGenerationContext context,
        JsonElement response,
        CancellationToken cancellationToken);
}

/// <summary>
/// The outcome of an <see cref="IAIResponseValidator"/> check.
/// </summary>
/// <param name="IsValid">Whether the response passed.</param>
/// <param name="Reason">Why the response failed; fed back to the model when it regenerates.</param>
public sealed record AIResponseValidationResult(bool IsValid, string? Reason)
{
    /// <summary>A passing result.</summary>
    public static AIResponseValidationResult Valid { get; } = new(true, null);

    /// <summary>Creates a failing result with the given reason.</summary>
    public static AIResponseValidationResult Invalid(string reason) => new(false, reason);
}
