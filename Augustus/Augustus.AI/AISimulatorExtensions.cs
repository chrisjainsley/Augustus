namespace Augustus.AI;

/// <summary>
/// Extension methods for adding AI-powered default handlers to an <see cref="APISimulator"/>.
/// </summary>
public static class AISimulatorExtensions
{
    /// <summary>
    /// Installs an AI-powered default handler that generates responses using OpenAI
    /// for requests that don't match any configured route.
    /// </summary>
    /// <param name="simulator">The API simulator instance.</param>
    /// <param name="options">AI configuration options including OpenAI API key and model settings.</param>
    /// <returns>The same <see cref="APISimulator"/> instance for method chaining.</returns>
    /// <remarks>
    /// When CacheOnly mode is active on the simulator, the AI default handler serves from cache only
    /// (returning HTTP 503 on cache miss) without requiring an API key.
    /// </remarks>
    public static APISimulator UseAI(this APISimulator simulator, AIOptions options)
    {
        if (simulator == null) throw new ArgumentNullException(nameof(simulator));
        if (options == null) throw new ArgumentNullException(nameof(options));

        // In cache-only mode, skip API key validation
        if (!simulator.Options.CacheOnly)
        {
            options.Validate();
        }

        var handler = new AIDefaultHandler(simulator, options);
        simulator.RoutingHandler.DefaultHandler = handler;
        return simulator;
    }

    /// <summary>
    /// Installs a proxy default handler that forwards unmatched requests to a real upstream API
    /// and caches the responses.
    /// </summary>
    /// <param name="simulator">The API simulator instance.</param>
    /// <param name="options">AI configuration options including API key for upstream authentication.
    /// When <see cref="AIOptions.OpenAIApiKey"/> is empty, the proxy forwards the caller's
    /// Authorization headers to the upstream instead of injecting its own.</param>
    /// <param name="upstreamUrl">The base URL of the real API to proxy to (e.g., "https://api.openai.com").</param>
    /// <returns>The same <see cref="APISimulator"/> instance for method chaining.</returns>
    /// <remarks>
    /// When CacheOnly mode is active on the simulator, the proxy serves from cache only
    /// (returning HTTP 503 on cache miss) without connecting to the upstream.
    /// </remarks>
    public static APISimulator UseProxy(this APISimulator simulator, AIOptions options, string upstreamUrl)
    {
        if (simulator == null) throw new ArgumentNullException(nameof(simulator));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(upstreamUrl)) throw new ArgumentException("Upstream URL is required", nameof(upstreamUrl));

        if (!simulator.Options.CacheOnly)
        {
            // Proxy mode: validate the API key only when one is provided.
            // When empty, the proxy forwards the caller's auth headers as-is.
            if (!string.IsNullOrEmpty(options.OpenAIApiKey))
            {
                options.Validate();
            }

            if (!Uri.IsWellFormedUriString(upstreamUrl, UriKind.Absolute))
            {
                throw new System.ComponentModel.DataAnnotations.ValidationException("Upstream URL must be a valid absolute URI");
            }
        }

        var handler = new ProxyDefaultHandler(simulator, options, upstreamUrl);
        simulator.RoutingHandler.DefaultHandler = handler;
        return simulator;
    }
}
