using System.ComponentModel.DataAnnotations;

namespace Augustus;

/// <summary>
/// Configuration options for the <see cref="APISimulator"/>.
/// </summary>
/// <remarks>
/// This class provides settings for OpenAI integration, caching behavior, and server configuration.
/// All properties have sensible defaults except <see cref="OpenAIApiKey"/>, which is required.
/// </remarks>
public class APISimulatorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether response caching is enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> to cache generated responses to disk for reuse; <c>false</c> to generate fresh responses for each request.
    /// Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// When enabled, responses are cached based on a hash of the request and instructions.
    /// This significantly reduces OpenAI API costs and improves test execution speed.
    /// Cached responses are stored in the folder specified by <see cref="CacheFolderPath"/>.
    /// </remarks>
    public bool EnableCaching { get; set; } = true;

    private string _cacheFolderPath = "./mocks";

    /// <summary>
    /// Gets or sets the file system path where cached responses are stored.
    /// </summary>
    /// <value>
    /// The path to the cache folder. Default is "./mocks".
    /// </value>
    /// <remarks>
    /// The path can be relative or absolute. If the folder doesn't exist, it will be created automatically.
    /// Setting this to null or whitespace will reset it to the default "./mocks" value.
    /// </remarks>
    public string CacheFolderPath
    {
        get => _cacheFolderPath;
        set => _cacheFolderPath = string.IsNullOrWhiteSpace(value) ? "./mocks" : value;
    }

    private string _openAIApiKey = string.Empty;

    /// <summary>
    /// Gets or sets the OpenAI API key used for generating responses.
    /// </summary>
    /// <value>
    /// The OpenAI API key. This property is required and must be set before starting the simulator.
    /// </value>
    /// <remarks>
    /// You can obtain an API key from https://platform.openai.com/api-keys.
    /// The value is automatically trimmed of leading/trailing whitespace.
    /// Consider loading this from environment variables or secure configuration rather than hardcoding.
    /// </remarks>
    /// <exception cref="ValidationException">Thrown during <see cref="Validate"/> if this property is not set.</exception>
    public string OpenAIApiKey
    {
        get => _openAIApiKey;
        set => _openAIApiKey = value?.Trim() ?? string.Empty;
    }

    private string _openAIEndpoint = string.Empty;

    /// <summary>
    /// Gets or sets a custom OpenAI API endpoint URL.
    /// </summary>
    /// <value>
    /// The custom API endpoint URL, or an empty string to use the default OpenAI endpoint.
    /// </value>
    /// <remarks>
    /// This is useful for Azure OpenAI Service or other OpenAI-compatible endpoints.
    /// If not set, the standard OpenAI API endpoint will be used.
    /// The value must be a valid absolute URI if provided.
    /// </remarks>
    /// <exception cref="ValidationException">Thrown during <see cref="Validate"/> if the value is not a valid absolute URI.</exception>
    public string OpenAIEndpoint
    {
        get => _openAIEndpoint;
        set => _openAIEndpoint = value?.Trim() ?? string.Empty;
    }

    private string _openAIModel = "gpt-5-mini";

    /// <summary>
    /// Gets or sets the OpenAI model to use for generating responses.
    /// </summary>
    /// <value>
    /// The model identifier. Default is "gpt-5-mini".
    /// </value>
    /// <remarks>
    /// Common values include "gpt-5", "gpt-5-mini", "gpt-5-nano", "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo".
    /// Different models have different costs and capabilities.
    /// Setting this to null or whitespace will reset it to "gpt-5-mini".
    /// </remarks>
    public string OpenAIModel
    {
        get => _openAIModel;
        set => _openAIModel = string.IsNullOrWhiteSpace(value) ? "gpt-5-mini" : value.Trim();
    }

    private int _port = 9001;

    /// <summary>
    /// Gets or sets the TCP port number on which the simulator will listen.
    /// </summary>
    /// <value>
    /// The port number. Must be between 1024 and 65535. Default is 9001.
    /// </value>
    /// <remarks>
    /// Ports below 1024 are typically reserved for system services and require elevated privileges.
    /// If the specified port is already in use, starting the simulator will fail.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 1024 or greater than 65535.</exception>
    public int Port
    {
        get => _port;
        set
        {
            if (value < 1024 || value > 65535)
                throw new ArgumentOutOfRangeException(nameof(Port), "Port must be between 1024 and 65535");
            _port = value;
        }
    }

    private int _maxRetries = 5;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for OpenAI API requests.
    /// </summary>
    /// <value>
    /// The maximum number of retries. Must be between 0 and 10. Default is 5.
    /// </value>
    /// <remarks>
    /// When OpenAI API requests fail due to rate limiting (HTTP 429) or transient errors (HTTP 500-504),
    /// the request will be retried up to this many times with exponential backoff.
    /// Set to 0 to disable retries.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 0 or greater than 10.</exception>
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

    private int _initialRetryDelayMs = 1000;

    /// <summary>
    /// Gets or sets the initial retry delay in milliseconds.
    /// </summary>
    /// <value>
    /// The initial delay in milliseconds before the first retry. Must be between 100 and 60000. Default is 1000 (1 second).
    /// </value>
    /// <remarks>
    /// The delay doubles with each subsequent retry (exponential backoff) up to <see cref="MaxRetryDelayMs"/>.
    /// For example, with default settings: 1s, 2s, 4s, 8s, 16s.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 100 or greater than 60000.</exception>
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

    private int _maxRetryDelayMs = 32000;

    /// <summary>
    /// Gets or sets the maximum retry delay in milliseconds.
    /// </summary>
    /// <value>
    /// The maximum delay in milliseconds between retry attempts. Must be between 1000 and 300000. Default is 32000 (32 seconds).
    /// </value>
    /// <remarks>
    /// This caps the exponential backoff delay to prevent excessively long waits.
    /// The delay starts at <see cref="InitialRetryDelayMs"/> and doubles with each retry until reaching this maximum.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 1000 or greater than 300000.</exception>
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

    private int _maxConcurrentRequests = 10;

    /// <summary>
    /// Gets or sets the maximum number of concurrent OpenAI API requests.
    /// </summary>
    /// <value>
    /// The maximum number of concurrent requests. Must be between 1 and 100. Default is 10.
    /// </value>
    /// <remarks>
    /// This limits how many requests can be in-flight to OpenAI at the same time.
    /// Requests beyond this limit will be queued and processed when slots become available.
    /// Lower values reduce the chance of hitting rate limits but may increase overall latency.
    /// Higher values allow more parallelism but increase the risk of rate limiting.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 1 or greater than 100.</exception>
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

    /// <summary>
    /// Validates that all required configuration is present and correct.
    /// </summary>
    /// <remarks>
    /// This method checks that:
    /// <list type="bullet">
    /// <item><description><see cref="OpenAIApiKey"/> is not empty</description></item>
    /// <item><description><see cref="OpenAIEndpoint"/>, if provided, is a valid absolute URI</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ValidationException">Thrown if any required configuration is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(OpenAIApiKey))
        {
            throw new ValidationException("OpenAI API key is required. Please set APISimulatorOptions.OpenAIApiKey");
        }

        if (!string.IsNullOrEmpty(OpenAIEndpoint) && !Uri.IsWellFormedUriString(OpenAIEndpoint, UriKind.Absolute))
        {
            throw new ValidationException("OpenAI endpoint must be a valid absolute URI");
        }
    }
}
