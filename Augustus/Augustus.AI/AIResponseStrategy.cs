namespace Augustus.AI;

using Augustus;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Response strategy that uses OpenAI (or Azure OpenAI) to generate realistic API responses for a matched route.
/// Uses the same cache key algorithm and on-disk cache as <see cref="AIDefaultHandler"/>.
/// </summary>
/// <remarks>
/// When the owning simulator has <see cref="APISimulatorOptions.CacheOnly"/> set, no API key is required and cache misses
/// return HTTP 503 without calling OpenAI. For standalone construction with a detached simulator, prefer
/// <see cref="DisposeAsync"/> (or <c>await using</c>) so the simulator is torn down asynchronously; <see cref="Dispose"/>
/// performs the same cleanup but may block briefly.
/// </remarks>
public sealed class AIResponseStrategy : IResponseStrategy, IDisposable, IAsyncDisposable
{
    private readonly APISimulator simulator;
    private readonly bool ownsDetachedSimulator;
    private readonly AIOptions options;
    private readonly List<string> instructions;
    private readonly IReadOnlyCollection<string> mergedDynamicFields;
    private readonly AIResponseGenerator? generator;

    /// <summary>
    /// Standalone constructor using <see cref="AIOptions.CacheFolderPath"/> for disk cache (no shared <see cref="APISimulator"/>).
    /// Prefer <see cref="RouteBuilderExtensions.UseAI"/> so cache keys and dynamic fields match the simulator.
    /// </summary>
    public AIResponseStrategy(AIOptions options, params string[] instructions)
        : this(
            CreateDetachedSimulator(options),
            options,
            instructions ?? Array.Empty<string>(),
            Array.Empty<string>(),
            ownsDetachedSimulator: true)
    {
    }

    internal AIResponseStrategy(
        APISimulator simulator,
        AIOptions options,
        string[] instructions,
        IReadOnlyCollection<string> mergedDynamicFields)
        : this(simulator, options, instructions, mergedDynamicFields, ownsDetachedSimulator: false)
    {
    }

    private AIResponseStrategy(
        APISimulator simulator,
        AIOptions options,
        string[] instructions,
        IReadOnlyCollection<string> mergedDynamicFields,
        bool ownsDetachedSimulator)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        this.ownsDetachedSimulator = ownsDetachedSimulator;
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.instructions = new List<string>(instructions ?? Array.Empty<string>());
        this.mergedDynamicFields = mergedDynamicFields ?? Array.Empty<string>();

        if (!simulator.Options.CacheOnly)
        {
            options.Validate();
            generator = new AIResponseGenerator(options);
        }
    }

    private static APISimulator CreateDetachedSimulator(AIOptions aiOptions)
    {
        var simOptions = new APISimulatorOptions
        {
            CacheFolderPath = aiOptions.CacheFolderPath,
            EnableCaching = aiOptions.EnableCaching,
            Port = 0,
            AutoRemoveStaleCache = false
        };
        return new APISimulator("DetachedAI", simOptions);
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var fileManager = simulator.CacheFileManager;
        var simOptions = simulator.Options;

        try
        {
            var bodyBytes = await httpContext.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);

            var path = httpContext.Request.Path.Value ?? "/";
            var requestHash = CacheKeyComputer.ComputeCacheKey(
                httpContext.Request.Method,
                path,
                httpContext.Request.QueryString.Value,
                bodyBytes,
                out var materializedBody,
                instructions,
                mergedDynamicFields);

            // Defer curl command generation until after primary cache check — avoids re-reading the
            // body stream, iterating headers, and string building on cache hits.
            string? curlRequest = null;

            if (simOptions.EnableCaching && options.EnableCaching)
            {
                var cachedEntry = await fileManager.ReadCachedEntryAsync(requestHash).ConfigureAwait(false);
                if (cachedEntry?.Response is not { Length: > 0 })
                {
                    // Legacy cache files were keyed on unfiltered cURL — use the original (no-skip-headers)
                    // overload so existing entries remain discoverable after DefaultAISkipHeaders was introduced.
                    var legacyCurl = await httpContext.Request.ToCurlCommandAsync().ConfigureAwait(false);
                    var legacyHash = CacheManager.GenerateLegacyCurlBasedCacheKey(legacyCurl, instructions);
                    cachedEntry = await fileManager.ReadCachedEntryAsync(legacyHash).ConfigureAwait(false);
                }

                if (cachedEntry?.Response is { Length: > 0 } cachedResponse)
                {
                    if (!cachedEntry.Normalized)
                    {
                        cachedResponse = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(cachedResponse, path);
                    }
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            if (simOptions.CacheOnly)
            {
                var message =
                    $"Cache-only mode: no cached response found for request hash '{requestHash}'. " +
                    "Run tests locally with an OpenAI API key to generate and cache this response.";
                if (options.CacheMissMaterializedBodyPrefixSha256ByteCount > 0)
                {
                    var n = Math.Min(options.CacheMissMaterializedBodyPrefixSha256ByteCount, materializedBody.Length);
                    if (n > 0)
                    {
                        var digest = SHA256.HashData(materializedBody.AsSpan(0, n));
                        message += $" Materialized body prefix SHA-256 (first {n} bytes): {Convert.ToHexString(digest)}.";
                    }
                }

                await WriteErrorResponse(httpContext, message, 503, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (generator is null)
            {
                await WriteErrorResponse(
                    httpContext,
                    "OpenAI client is not initialized. Provide an API key or enable cache-only mode.",
                    500,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            if (instructions.Count == 0)
            {
                await WriteErrorResponse(
                    httpContext,
                    "No instructions provided. Please add instructions using WithInstruction() or the UseAI overload.",
                    500,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            // Generate curl command if not already resolved during legacy cache lookup.
            curlRequest ??= await httpContext.Request.ToCurlCommandAsync(HttpRequestExtensions.DefaultAISkipHeaders).ConfigureAwait(false);
            // Sanitize any residual sensitive values (query params, body tokens) before forwarding to OpenAI.
            curlRequest = SensitiveDataSanitizer.SanitizeSensitiveValues(curlRequest);

            var generation = await generator
                .GenerateAsync(requestHash, new AIGenerationContext(httpContext.Request.Method, path, curlRequest, instructions), cancellationToken)
                .ConfigureAwait(false);

            if (!generation.IsSuccess)
            {
                await WriteErrorResponse(httpContext, generation.Failure!, 502, cancellationToken, generation.Attempts).ConfigureAwait(false);
                return;
            }

            var responseContent = generation.Body!;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(responseContent, cancellationToken).ConfigureAwait(false);

            if (simOptions.EnableCaching && options.EnableCaching)
            {
                try
                {
                    await fileManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions, normalized: true, model: generation.Model).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAI request failed: {ex}");
            await WriteErrorResponse(httpContext, "Failed to generate response from OpenAI API", 502, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAI request timeout: {ex}");
            await WriteErrorResponse(httpContext, "Request timeout while contacting OpenAI API", 504, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex is not TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"Operation cancelled: {ex}");
            await WriteErrorResponse(httpContext, "Request cancelled", 499, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error generating response: {ex}");
            await WriteErrorResponse(httpContext, "Internal server error", 500, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken, int? attempts = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = attempts is null
            ? JsonSerializer.Serialize(new { error = message ?? "Unknown error", status = statusCode })
            : JsonSerializer.Serialize(new { error = message ?? "Unknown error", status = statusCode, attempts });
        await context.Response.WriteAsync(errorResponse, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (ownsDetachedSimulator)
        {
            simulator.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Asynchronously releases resources, including the detached simulator when this strategy owns it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (ownsDetachedSimulator)
        {
            await simulator.DisposeAsync().ConfigureAwait(false);
        }
    }
}
