namespace Augustus.AI;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration options for AI-powered response generation.
/// </summary>
public class AIOptions
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
    /// Gets or sets the maximum number of concurrent OpenAI API requests.
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
    }
}
