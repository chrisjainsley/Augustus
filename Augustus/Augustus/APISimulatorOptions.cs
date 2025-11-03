namespace Augustus;

/// <summary>
/// Configuration options for the <see cref="APISimulator"/>.
/// </summary>
public class APISimulatorOptions
{
    private int _port;
    private int _maxRetries = 5;
    private int _initialRetryDelayMs = 1000;
    private int _maxRetryDelayMs = 32000;
    private int _maxConcurrentRequests = 10;

    /// <summary>
    /// Random number generator for port allocation.
    /// </summary>
    private static readonly Random portRandom = new Random();
    private static readonly object portLock = new object();

    /// <summary>
    /// Generates a random port in the range 10000-59999 to avoid conflicts.
    /// Ensures the port is not already in use by attempting to bind to it.
    /// </summary>
    private static int GenerateRandomPort()
    {
        lock (portLock)
        {
            int maxAttempts = 100;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                int port = portRandom.Next(10000, 60000);

                // Try to bind to the port to check if it's available
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                try
                {
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    // Port is in use, try again
                    attempts++;
                }
            }

            // Fallback: if we can't find an available port after many attempts, just return a random one
            // The actual binding will fail later with a more descriptive error
            return portRandom.Next(10000, 60000);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="APISimulatorOptions"/> class.
    /// </summary>
    public APISimulatorOptions()
    {
        // Auto-assign a random port by default to avoid conflicts in tests
        _port = GenerateRandomPort();
    }

    /// <summary>
    /// Gets or sets the TCP port number on which the API simulator will listen.
    /// </summary>
    /// <value>
    /// The port number. Must be 0 (auto-assign) or between 1024 and 65535.
    /// By default, a random port is automatically assigned at construction time to avoid conflicts.
    /// </value>
    /// <remarks>
    /// Ports below 1024 are typically reserved for system services and require elevated privileges.
    /// If the specified port is already in use, starting the simulator will fail.
    /// Each new APISimulatorOptions instance automatically gets a random port assignment to avoid conflicts,
    /// which is especially useful for running parallel tests.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 1024 or greater than 65535.</exception>
    public int Port
    {
        get => _port;
        set
        {
            if (value != 0 && (value < 1024 || value > 65535))
                throw new ArgumentOutOfRangeException(nameof(Port), "Port must be 0 (auto-assign) or between 1024 and 65535");
            _port = value;
        }
    }

    /// <summary>
    /// Gets or sets the OpenAI API key for AI-powered responses.
    /// </summary>
    /// <value>
    /// The API key string. Cannot be null or empty when used with AI features.
    /// </value>
    public string OpenAIApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether response caching is enabled.
    /// </summary>
    /// <value>
    /// true to enable caching; false to disable. Default is true.
    /// </value>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the folder path where cached responses are stored.
    /// </summary>
    /// <value>
    /// The folder path. Default is "./mocks".
    /// </value>
    public string CacheFolderPath { get; set; } = "./mocks";

    /// <summary>
    /// Gets or sets the OpenAI model to use for API calls.
    /// </summary>
    /// <value>
    /// The model identifier (e.g., "gpt-5-mini"). Default is "gpt-5-mini".
    /// </value>
    public string OpenAIModel { get; set; } = "gpt-5-mini";

    /// <summary>
    /// Gets or sets the custom OpenAI endpoint URL.
    /// </summary>
    /// <value>
    /// The endpoint URL. Default is empty string.
    /// </value>
    public string OpenAIEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for API calls.
    /// </summary>
    /// <value>
    /// The maximum number of retries. Must be between 0 and 10. Default is 5.
    /// </value>
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

    /// <summary>
    /// Gets or sets the initial delay in milliseconds before the first retry attempt.
    /// </summary>
    /// <value>
    /// The delay in milliseconds. Must be between 100 and 60000. Default is 1000.
    /// </value>
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

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds between retry attempts.
    /// </summary>
    /// <value>
    /// The delay in milliseconds. Must be between 1000 and 300000. Default is 32000.
    /// </value>
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

    /// <summary>
    /// Gets or sets the maximum number of concurrent API requests allowed.
    /// </summary>
    /// <value>
    /// The maximum number of concurrent requests. Must be between 1 and 100. Default is 10.
    /// </value>
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
}
