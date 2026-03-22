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
    private string _openAIModel = "gpt-4o-mini";
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
    /// Gets or sets a custom OpenAI API endpoint URL (optional, for Azure OpenAI or custom endpoints).
    /// </summary>
    public string OpenAIEndpoint
    {
        get => _openAIEndpoint;
        set => _openAIEndpoint = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the OpenAI model to use for generating responses.
    /// </summary>
    public string OpenAIModel
    {
        get => _openAIModel;
        set => _openAIModel = string.IsNullOrWhiteSpace(value) ? "gpt-4o-mini" : value.Trim();
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
