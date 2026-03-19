namespace Augustus.AI;

using Augustus;

/// <summary>
/// Extension methods for adding AI capabilities to Augustus route builders.
/// </summary>
public static class RouteBuilderExtensions
{
    /// <summary>
    /// Configures this route to use OpenAI for generating responses.
    /// </summary>
    /// <param name="builder">The route builder.</param>
    /// <param name="options">AI configuration options.</param>
    /// <param name="instructions">Instructions to guide the AI response generation.</param>
    /// <returns>The route builder for method chaining.</returns>
    public static RouteBuilder UseAI(this RouteBuilder builder, AIOptions options, params string[] instructions)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var strategy = new AIResponseStrategy(options, instructions);
        return builder.WithStrategy(strategy);
    }

    /// <summary>
    /// Configures this route to use OpenAI for generating responses with a single instruction.
    /// </summary>
    /// <param name="builder">The route builder.</param>
    /// <param name="options">AI configuration options.</param>
    /// <param name="instruction">Instruction to guide the AI response generation.</param>
    /// <returns>The route builder for method chaining.</returns>
    public static RouteBuilder UseAI(this RouteBuilder builder, AIOptions options, string instruction)
    {
        return builder.UseAI(options, new[] { instruction });
    }

    /// <summary>
    /// Configures this route to proxy requests to a real API and cache the responses.
    /// </summary>
    /// <param name="builder">The route builder.</param>
    /// <param name="baseUrl">The base URL of the real API to proxy to.</param>
    /// <param name="options">AI configuration options (for caching).</param>
    /// <param name="headers">Optional HTTP headers to include in proxied requests.</param>
    /// <returns>The route builder for method chaining.</returns>
    public static RouteBuilder UseRealApi(this RouteBuilder builder, string baseUrl, AIOptions? options = null, Dictionary<string, string>? headers = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        var strategy = new RealApiProxyStrategy(baseUrl, options, headers);
        return builder.WithStrategy(strategy);
    }

    /// <summary>
    /// Configures this route to proxy requests to a real API with an API key.
    /// </summary>
    /// <param name="builder">The route builder.</param>
    /// <param name="baseUrl">The base URL of the real API to proxy to.</param>
    /// <param name="apiKey">The API key to include in the Authorization header.</param>
    /// <param name="options">AI configuration options (for caching).</param>
    /// <returns>The route builder for method chaining.</returns>
    public static RouteBuilder UseRealApi(this RouteBuilder builder, string baseUrl, string apiKey, AIOptions? options = null)
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {apiKey}" }
        };
        return builder.UseRealApi(baseUrl, options, headers);
    }
}
