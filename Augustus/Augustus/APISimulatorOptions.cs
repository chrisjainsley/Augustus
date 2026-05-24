namespace Augustus;

/// <summary>
/// Configuration options for the <see cref="APISimulator"/>.
/// </summary>
/// <remarks>
/// This class provides settings for caching behavior and server configuration.
/// For AI-powered response generation, install the Augustus.AI package and use
/// <c>AIOptions</c> with the <c>UseAI</c> extension method.
/// </remarks>
public sealed class APISimulatorOptions
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

    private bool _cacheOnly;

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
    public bool CacheOnly
    {
        get => _cacheOnly;
        set
        {
            _cacheOnly = value;
            if (value)
            {
                EnableCaching = true;
                AutoRemoveStaleCache = false;
            }
        }
    }

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
    /// Gets or sets a transform applied to the <see cref="RequestKey"/> before hashing and
    /// matching. Use it to strip or rewrite volatile parts of the request (path segments,
    /// query values) so incidental changes do not invalidate committed fixtures. The same
    /// transform is applied to incoming requests and re-applied to each fixture's stored
    /// <see cref="CanonicalRequest"/>, so a keying-rule change re-baselines with zero
    /// upstream calls. Must be pure, deterministic, and idempotent (<c>f(f(x)) == f(x)</c>).
    /// </summary>
    public Func<RequestKey, RequestKey>? RequestKeyTransform { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the volatile Azure OpenAI deployment path
    /// segment (<c>/openai/deployments/{deployment}/…</c>) is replaced with a constant
    /// before hashing, making cache keys deployment- and region-agnostic. Default is
    /// <c>false</c>. Applied before <see cref="RequestKeyTransform"/>.
    /// </summary>
    public bool NormalizeAzureOpenAIDeployment { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked on every cache miss with the canonical request and
    /// computed key Augustus expected. Use it to discover the exact identity needed to
    /// author or rename a fixture by hand when the upstream API is unavailable.
    /// </summary>
    public Action<CacheMissDiagnostic>? OnCacheMiss { get; set; }

    private readonly List<string> _ignoredHeaders = new();
    private readonly List<string> _ignoredQueryParameters = new();

    /// <summary>
    /// Gets a list of request header names excluded from the request forwarded to the AI
    /// model when generating a response. Cache identity never includes headers; use this
    /// to keep volatile headers out of the AI prompt so they do not influence generation.
    /// </summary>
    public IList<string> IgnoredHeaders => _ignoredHeaders;

    /// <summary>
    /// Gets a list of query-string parameter names removed before the cache key is
    /// computed, so volatile parameters (cache-busters, timestamps) do not invalidate
    /// committed fixtures.
    /// </summary>
    public IList<string> IgnoredQueryParameters => _ignoredQueryParameters;

    /// <summary>
    /// Gets or sets a value indicating whether JSON body properties whose value is
    /// <c>null</c> are removed before the cache key is computed, so an explicit-null vs.
    /// omitted-property difference does not churn the cache. Default is <c>false</c>.
    /// </summary>
    public bool StripNullBodyProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only <c>messages[].content</c> participates
    /// in the cache key, so cosmetic changes to other body fields (temperature, metadata)
    /// do not churn caches. Default is <c>false</c>.
    /// </summary>
    public bool HashMessagesContentOnly { get; set; }

    /// <summary>
    /// On cache HIT for a legacy fixture (no <see cref="CanonicalRequest"/>), rewrite the
    /// file in place with the freshly computed canonical request so it can later be
    /// renamed offline via <see cref="CacheMaintenance.Rekey"/>. The on-disk filename is
    /// not changed by the backfill; only the JSON body gains the missing
    /// <c>CanonicalRequest</c> block. Idempotent — a fixture that already has a
    /// CanonicalRequest is never rewritten. Default is <c>false</c>, preserving
    /// byte-identical behavior for legacy fixtures.
    /// </summary>
    /// <remarks>
    /// Use this for one-shot migration of pre-canonical-request fixtures: enable it,
    /// run the existing test suite in proxy mode with the historical keying rules so
    /// every request still hits the legacy filename, and the backfill embeds
    /// CanonicalRequest into each fixture without any upstream API calls. After the
    /// migration pass, disable it and enable the keying-rule knobs you want
    /// (<see cref="NormalizeAzureOpenAIDeployment"/>, <see cref="RequestKeyTransform"/>,
    /// etc.), then call <see cref="CacheMaintenance.Rekey"/> to rename files under the
    /// new rules.
    /// </remarks>
    public bool BackfillLegacyCanonicalRequest { get; set; }

    private readonly List<string> _dynamicContentFields = new();

    /// <summary>
    /// Gets a list of JSON property names whose values should be normalized
    /// (replaced with a constant) when computing cache keys.
    /// </summary>
    /// <remarks>
    /// Use this to ignore dynamic content such as GUIDs, tool_call_ids, or timestamps
    /// so that logically identical requests produce the same cache key across test runs.
    /// Add items via the <see cref="ICollection{T}.Add"/> method on the returned list.
    /// </remarks>
    public IList<string> DynamicContentFields => _dynamicContentFields;

    /// <summary>
    /// Builds the normalization rules applied when computing and matching cache keys.
    /// The default (no transform, no knobs) reproduces the historical key byte-for-byte.
    /// </summary>
    internal CacheKeyRules BuildCacheKeyRules() => CacheKeyRules.From(
        _dynamicContentFields,
        _ignoredQueryParameters,
        RequestKeyTransform,
        NormalizeAzureOpenAIDeployment,
        StripNullBodyProperties,
        HashMessagesContentOnly);

    /// <summary>
    /// Validates that all required configuration is present and correct.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CacheOnly"/> is <c>true</c> but <see cref="EnableCaching"/> is <c>false</c>
    /// or <see cref="AutoRemoveStaleCache"/> is <c>true</c>, indicating an invalid configuration.
    /// </exception>
    public void Validate()
    {
        if (_cacheOnly)
        {
            if (!EnableCaching)
                throw new InvalidOperationException("CacheOnly requires EnableCaching to be true.");
            if (AutoRemoveStaleCache)
                throw new InvalidOperationException("CacheOnly requires AutoRemoveStaleCache to be false.");
        }
    }
}
