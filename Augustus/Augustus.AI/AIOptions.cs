using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Augustus.AI.Tests")]

namespace Augustus.AI;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration options for AI-powered response generation.
/// </summary>
public sealed class AIOptions
{
    private string _openAIApiKey = string.Empty;
    private string _openAIEndpoint = string.Empty;
    /// <summary>The model used when <see cref="OpenAIModel"/> is not set.</summary>
    internal const string DefaultOpenAIModel = "gpt-6-luna";

    /// <summary>The reasoning effort applied to <see cref="DefaultOpenAIModel"/> when none is set.</summary>
    internal const string DefaultModelReasoningEffort = "none";

    internal static readonly IReadOnlyList<string> ReasoningEffortLevels = new[] { "none", "minimal", "low", "medium", "high" };

    private const int MaxModelTiers = 10;
    private const int MaxOutputTokensLimit = 32768;
    private const int DefaultMaxValidationRetries = 3;
    private const int MaxValidationRetriesLimit = 5;

    private string _openAIModel = DefaultOpenAIModel;
    private string _cacheFolderPath = "./mocks";
    private int _maxRetries = 5;
    private int _initialRetryDelayMs = 1000;
    private int _maxRetryDelayMs = 32000;
    private int _maxConcurrentRequests = 10;

    /// <summary>
    /// Gets or sets the OpenAI API key.
    /// </summary>
    public string OpenAIApiKey
    {
        get => _openAIApiKey;
        set => _openAIApiKey = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets a custom endpoint URL: the Azure OpenAI resource when <see cref="UseAzureOpenAI"/> is set,
    /// otherwise any OpenAI-compatible API (for example <c>https://api.groq.com/openai/v1</c>).
    /// </summary>
    public string OpenAIEndpoint
    {
        get => _openAIEndpoint;
        set => _openAIEndpoint = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the OpenAI model to use for generating responses. Defaults to <c>gpt-6-luna</c>.
    /// </summary>
    public string OpenAIModel
    {
        get => _openAIModel;
        set => _openAIModel = string.IsNullOrWhiteSpace(value) ? DefaultOpenAIModel : value.Trim();
    }

    private string? _reasoningEffort;
    private bool _reasoningEffortSet;

    /// <summary>
    /// Gets or sets the reasoning effort sent with each request: <c>none</c>, <c>minimal</c>, <c>low</c>,
    /// <c>medium</c> or <c>high</c>. Null sends no reasoning effort, which non-reasoning models require.
    /// </summary>
    /// <remarks>
    /// When not set, this is <c>none</c> while <see cref="OpenAIModel"/> is the default model and null otherwise,
    /// so setting a non-reasoning model such as <c>gpt-4o-mini</c> never sends an unsupported parameter.
    /// </remarks>
    public string? ReasoningEffort
    {
        get => _reasoningEffortSet
            ? _reasoningEffort
            : !UseAzureOpenAI && _openAIModel == DefaultOpenAIModel ? DefaultModelReasoningEffort : null;
        set
        {
            _reasoningEffort = NormalizeReasoningEffort(value, nameof(ReasoningEffort));
            _reasoningEffortSet = true;
        }
    }

    private int? _maxOutputTokens;

    /// <summary>
    /// Gets or sets the maximum number of tokens a generated response may use. Null leaves the provider default.
    /// </summary>
    public int? MaxOutputTokens
    {
        get => _maxOutputTokens;
        set
        {
            if (value is < 1 or > MaxOutputTokensLimit)
                throw new ArgumentOutOfRangeException(nameof(MaxOutputTokens), $"MaxOutputTokens must be between 1 and {MaxOutputTokensLimit}");
            _maxOutputTokens = value;
        }
    }

    /// <summary>
    /// Gets the model tiers used to generate responses, weakest first. When empty, the tiers are
    /// <see cref="OpenAIModel"/> at each reasoning effort from <see cref="ReasoningEffort"/> up to <c>high</c>,
    /// or <see cref="OpenAIModel"/> alone when <see cref="ReasoningEffort"/> is null.
    /// </summary>
    public IList<AIModelTier> ModelTiers { get; } = new List<AIModelTier>();

    /// <summary>
    /// Gets or sets the router that picks the starting tier for each generated response.
    /// When null, generation starts at the first tier.
    /// </summary>
    public IAIModelRouter? ModelRouter { get; set; }

    /// <summary>
    /// Gets the validators every freshly generated response must pass before it is returned or cached.
    /// Responses that are not valid JSON are always rejected.
    /// </summary>
    public IList<IAIResponseValidator> ResponseValidators { get; } = new List<IAIResponseValidator>();

    private int _maxValidationRetries = DefaultMaxValidationRetries;

    /// <summary>
    /// Gets or sets how many times a rejected response is regenerated, one tier higher each time,
    /// before the request fails with HTTP 502.
    /// </summary>
    public int MaxValidationRetries
    {
        get => _maxValidationRetries;
        set
        {
            if (value < 0 || value > MaxValidationRetriesLimit)
                throw new ArgumentOutOfRangeException(nameof(MaxValidationRetries), $"MaxValidationRetries must be between 0 and {MaxValidationRetriesLimit}");
            _maxValidationRetries = value;
        }
    }

    /// <summary>Test hook: replaces the HTTP transport for model calls.</summary>
    internal HttpMessageHandler? HttpHandlerOverride { get; set; }

    /// <summary>
    /// Returns <see cref="ModelTiers"/>, or the reasoning-effort ladder built from <see cref="OpenAIModel"/>
    /// (or <see cref="AzureDeploymentName"/>) when no tiers are configured.
    /// </summary>
    internal IReadOnlyList<AIModelTier> ResolveModelTiers()
    {
        if (ModelTiers.Count > 0)
            return ModelTiers.ToList();

        var model = UseAzureOpenAI ? AzureDeploymentName : OpenAIModel;
        var effort = ReasoningEffort;
        if (effort is null)
            return new[] { new AIModelTier(model, EffortLadderDescriptions[0]) };

        var start = ReasoningEffortLevels.ToList().IndexOf(effort);
        // "none" and "minimal" are alternatives at the bottom of the ladder; both climb through low, medium, high.
        var ladder = new List<string> { effort };
        ladder.AddRange(ReasoningEffortLevels.Skip(Math.Max(start + 1, 2)));
        return ladder
            .Select((level, index) => new AIModelTier(model, EffortLadderDescriptions[Math.Min(index, EffortLadderDescriptions.Length - 1)])
            {
                ReasoningEffort = level
            })
            .ToList();
    }

    private static readonly string[] EffortLadderDescriptions =
    {
        "A single resource whose fields come straight from the request or the instructions.",
        "A few related or nested objects, or light conditional logic in the instructions.",
        "Lists, computed totals, or several instructions that interact.",
        "Many interacting rules or multi-step reasoning needed to produce a correct response."
    };

    private static string? NormalizeReasoningEffort(string? value, string parameterName)
    {
        if (value is null)
            return null;

        var normalized = value.Trim().ToLowerInvariant();
        if (!ReasoningEffortLevels.Contains(normalized))
            throw new ArgumentOutOfRangeException(parameterName, $"Reasoning effort must be one of: {string.Join(", ", ReasoningEffortLevels)}");
        return normalized;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use Azure OpenAI service instead of standard OpenAI.
    /// </summary>
    public bool UseAzureOpenAI { get; set; } = false;

    private string _azureDeploymentName = string.Empty;

    /// <summary>
    /// Gets or sets the Azure OpenAI deployment name.
    /// Required when <see cref="UseAzureOpenAI"/> is <c>true</c>.
    /// </summary>
    public string AzureDeploymentName
    {
        get => _azureDeploymentName;
        set => _azureDeploymentName = value?.Trim() ?? string.Empty;
    }

    private string _azureApiVersion = "2024-06-01";

    /// <summary>
    /// Gets or sets the Azure OpenAI API version.
    /// </summary>
    public string AzureApiVersion
    {
        get => _azureApiVersion;
        set => _azureApiVersion = string.IsNullOrWhiteSpace(value) ? "2024-06-01" : value.Trim();
    }

    /// <summary>
    /// Gets or sets a value indicating whether response caching is enabled.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the file system path where cached responses are stored.
    /// </summary>
    public string CacheFolderPath
    {
        get => _cacheFolderPath;
        set => _cacheFolderPath = string.IsNullOrWhiteSpace(value) ? "./mocks" : value;
    }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for OpenAI API requests.
    /// </summary>
    public int MaxRetries
    {
        get => _maxRetries;
        set
        {
            if (value < 0 || value > 10)
                throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries must be between 0 and 10");
            _maxRetries = value;
        }
    }

    /// <summary>
    /// Gets or sets the initial retry delay in milliseconds.
    /// </summary>
    public int InitialRetryDelayMs
    {
        get => _initialRetryDelayMs;
        set
        {
            if (value < 100 || value > 60000)
                throw new ArgumentOutOfRangeException(nameof(InitialRetryDelayMs), "InitialRetryDelayMs must be between 100 and 60000");
            _initialRetryDelayMs = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum retry delay in milliseconds.
    /// </summary>
    public int MaxRetryDelayMs
    {
        get => _maxRetryDelayMs;
        set
        {
            if (value < 1000 || value > 300000)
                throw new ArgumentOutOfRangeException(nameof(MaxRetryDelayMs), "MaxRetryDelayMs must be between 1000 and 300000");
            _maxRetryDelayMs = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of concurrent OpenAI chat completion requests per process for this
    /// credential, model/deployment, and concurrency setting. Shared across default and route-level AI handlers.
    /// </summary>
    public int MaxConcurrentRequests
    {
        get => _maxConcurrentRequests;
        set
        {
            if (value < 1 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(MaxConcurrentRequests), "MaxConcurrentRequests must be between 1 and 100");
            _maxConcurrentRequests = value;
        }
    }

    private int _proxyTimeoutSeconds = 120;

    private int _cacheMissMaterializedBodyPrefixSha256ByteCount;

    /// <summary>
    /// When greater than 0 and a cache miss occurs in cache-only mode, includes a SHA-256 digest (64-character hex)
    /// of the first N bytes of the materialized request body used for the cache key.
    /// Raw body bytes are never included in the response, avoiding accidental exposure of secrets or PII.
    /// </summary>
    /// <remarks>
    /// Proxy JSON adds <c>materializedBodyPrefixSha256</c>; the AI default handler appends the same digest to the error text.
    /// Use only for local or controlled debugging (e.g. comparing Linux vs Windows); keep <c>0</c> (default) in production.
    /// </remarks>
    public int CacheMissMaterializedBodyPrefixSha256ByteCount
    {
        get => _cacheMissMaterializedBodyPrefixSha256ByteCount;
        set
        {
            if (value < 0 || value > 4096)
                throw new ArgumentOutOfRangeException(nameof(CacheMissMaterializedBodyPrefixSha256ByteCount), "Value must be between 0 and 4096.");
            _cacheMissMaterializedBodyPrefixSha256ByteCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the timeout in seconds for upstream proxy requests.
    /// </summary>
    public int ProxyTimeoutSeconds
    {
        get => _proxyTimeoutSeconds;
        set
        {
            if (value < 1 || value > 600)
                throw new ArgumentOutOfRangeException(nameof(ProxyTimeoutSeconds), "ProxyTimeoutSeconds must be between 1 and 600");
            _proxyTimeoutSeconds = value;
        }
    }

    /// <summary>
    /// Validates that all required configuration is present and correct.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if any required configuration is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(OpenAIApiKey))
        {
            throw new ValidationException("OpenAI API key is required. Please set AIOptions.OpenAIApiKey");
        }

        if (!string.IsNullOrEmpty(OpenAIEndpoint) && !Uri.IsWellFormedUriString(OpenAIEndpoint, UriKind.Absolute))
        {
            throw new ValidationException("OpenAI endpoint must be a valid absolute URI");
        }

        if (ModelTiers.Count > MaxModelTiers)
        {
            throw new ValidationException($"At most {MaxModelTiers} model tiers are supported.");
        }

        foreach (var tier in ModelTiers)
        {
            if (tier is null || string.IsNullOrWhiteSpace(tier.Model))
                throw new ValidationException("Every model tier needs a model name.");
            if (tier.Endpoint is not null && !Uri.IsWellFormedUriString(tier.Endpoint, UriKind.Absolute))
                throw new ValidationException($"Model tier '{tier.Model}' endpoint must be a valid absolute URI");
            if (tier.ReasoningEffort is not null && !ReasoningEffortLevels.Contains(tier.ReasoningEffort))
                throw new ValidationException($"Model tier '{tier.Model}' reasoning effort must be one of: {string.Join(", ", ReasoningEffortLevels)}");
        }

        if (UseAzureOpenAI)
        {
            if (string.IsNullOrWhiteSpace(OpenAIEndpoint))
            {
                throw new ValidationException("OpenAI endpoint is required when UseAzureOpenAI is true. Please set AIOptions.OpenAIEndpoint to your Azure OpenAI resource URL (e.g., https://your-resource.openai.azure.com)");
            }

            if (string.IsNullOrWhiteSpace(AzureDeploymentName))
            {
                throw new ValidationException("Azure deployment name is required when UseAzureOpenAI is true. Please set AIOptions.AzureDeploymentName");
            }
        }
    }
}
