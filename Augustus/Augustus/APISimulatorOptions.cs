namespace Augustus;

/// <summary>
/// Configuration options for the <see cref="APISimulator"/>.
/// </summary>
/// <remarks>
/// This class provides settings for caching behavior and server configuration.
/// For AI-powered response generation, install the Augustus.AI package and use
/// <c>AIOptions</c> with the <c>UseAI</c> extension method.
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
    /// When enabled, responses are cached based on a hash of the request.
    /// Cached responses are stored in the folder specified by <see cref="CacheFolderPath"/>.
    /// </remarks>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether stale cache entries are automatically removed on dispose.
    /// </summary>
    /// <value>
    /// <c>true</c> to automatically delete cache files that were not accessed during the simulator's lifetime;
    /// <c>false</c> to keep all cache files. Default is <c>true</c>.
    /// </value>
    public bool AutoRemoveStaleCache { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the simulator operates in cache-only mode.
    /// </summary>
    /// <value>
    /// <c>true</c> to serve only cached responses (cache misses return HTTP 503);
    /// <c>false</c> for normal operation. Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// When enabled, forces <see cref="EnableCaching"/> to <c>true</c> and
    /// <see cref="AutoRemoveStaleCache"/> to <c>false</c>.
    /// Useful for CI environments where responses are pre-cached.
    /// </remarks>
    public bool CacheOnly { get; set; } = false;

    private string _cacheFolderPath = "./mocks";
    private bool _cacheFolderPathExplicitlySet;

    /// <summary>
    /// Gets or sets the file system path where cached responses are stored.
    /// </summary>
    /// <value>
    /// The path to the cache folder. Default is "./mocks".
    /// </value>
    /// <remarks>
    /// The path can be relative or absolute. If the folder doesn't exist, it will be created automatically.
    /// Setting this to null or whitespace will reset it to the default "./mocks" value.
    /// When explicitly set, this overrides the per-test-class auto-resolution from <c>[CallerFilePath]</c>.
    /// </remarks>
    public string CacheFolderPath
    {
        get => _cacheFolderPath;
        set
        {
            _cacheFolderPath = string.IsNullOrWhiteSpace(value) ? "./mocks" : value;
            _cacheFolderPathExplicitlySet = true;
        }
    }

    internal bool IsCacheFolderPathExplicitlySet => _cacheFolderPathExplicitlySet;

    internal void ResetCacheFolderPathExplicitFlag() => _cacheFolderPathExplicitlySet = false;

    internal string? TestClassFilePath { get; set; }

    internal static string ResolveCacheFolderPath(string? testClassFilePath, string apiName, string defaultPath)
    {
        if (string.IsNullOrEmpty(testClassFilePath))
            return defaultPath;

        var dir = Path.GetDirectoryName(testClassFilePath)!;
        var className = Path.GetFileNameWithoutExtension(testClassFilePath);
        return Path.Combine(dir, "__mocks__", className, apiName);
    }

    private int _port = 9001;

    /// <summary>
    /// Gets or sets the TCP port number on which the simulator will listen.
    /// </summary>
    /// <value>
    /// The port number. Must be 0 (auto-assign) or between 1024 and 65535. Default is 9001.
    /// </value>
    /// <remarks>
    /// Set to 0 to let the OS auto-assign an available port — useful for parallel test execution.
    /// Ports below 1024 are typically reserved for system services and require elevated privileges.
    /// If the specified port is already in use, starting the simulator will fail.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than 0 or greater than 65535, or between 1 and 1023.</exception>
    public int Port
    {
        get => _port;
        set
        {
            if (value < 0 || value > 65535 || (value > 0 && value < 1024))
                throw new ArgumentOutOfRangeException(nameof(Port), "Port must be 0 (auto-assign) or between 1024 and 65535");
            _port = value;
        }
    }

    /// <summary>
    /// Gets or sets a list of JSON property names whose values should be normalized
    /// (replaced with a constant) when computing cache keys.
    /// </summary>
    /// <remarks>
    /// Use this to ignore dynamic content such as GUIDs, tool_call_ids, or timestamps
    /// so that logically identical requests produce the same cache key across test runs.
    /// </remarks>
    public List<string> DynamicContentFields { get; set; } = new();

    /// <summary>
    /// Validates that all required configuration is present and correct.
    /// </summary>
    /// <remarks>
    /// Enforces cache-only mode invariants when enabled.
    /// </remarks>
    public void Validate()
    {
        if (CacheOnly)
        {
            EnableCaching = true;
            AutoRemoveStaleCache = false;
        }
    }
}
