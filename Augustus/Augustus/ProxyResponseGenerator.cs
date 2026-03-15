using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Augustus;

internal class ProxyResponseGenerator : IResponseGenerator, IDisposable
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Transfer-Encoding", "TE",
        "Trailer", "Upgrade", "Proxy-Authorization", "Proxy-Authenticate"
    };

    private static readonly List<string> EmptyInstructions = new();

    private readonly APISimulatorOptions options;
    private readonly APISimulator.FileManager fileManager;
    private readonly BackgroundCacheWriter cacheWriter;
    private readonly HttpClient? upstreamClient;

    public ProxyResponseGenerator(APISimulatorOptions options, APISimulator.FileManager fileManager)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        this.cacheWriter = new BackgroundCacheWriter(fileManager);

        if (!options.CacheOnly)
        {
            upstreamClient = new HttpClient
            {
                BaseAddress = new Uri(options.OpenAIEndpoint)
            };
        }
    }

    public async Task GenerateResponse(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var bodyBytes = await httpContext.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);

            var method = httpContext.Request.Method;
            var path = httpContext.Request.Path.Value ?? "/";
            var queryString = httpContext.Request.QueryString.Value;

            var requestHash = CacheKeyComputer.ComputeCacheKey(method, path, queryString, bodyBytes);

            // Try cache first
            if (options.EnableCaching)
            {
                var cachedResponse = await fileManager.ReadCachedResponseAsync(requestHash).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken);
                    return;
                }
            }

            // Cache miss in CacheOnly mode → 503
            if (options.CacheOnly)
            {
                httpContext.Response.StatusCode = 503;
                httpContext.Response.ContentType = "application/json";
                var error = JsonSerializer.Serialize(new
                {
                    error = "Cache miss in CacheOnly mode. No cached response found for this request.",
                    status = 503,
                    requestHash
                });
                await httpContext.Response.WriteAsync(error, cancellationToken);
                return;
            }

            // Forward request to upstream
            var upstreamRequest = new HttpRequestMessage(new HttpMethod(method), path + httpContext.Request.QueryString);
            upstreamRequest.Content = new ByteArrayContent(bodyBytes);

            // Copy relevant headers
            foreach (var header in httpContext.Request.Headers)
            {
                if (HopByHopHeaders.Contains(header.Key) || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    upstreamRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                else
                {
                    upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            var upstreamResponse = await upstreamClient!.SendAsync(upstreamRequest, cancellationToken).ConfigureAwait(false);
            var responseContent = await upstreamResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // Write response to client
            httpContext.Response.StatusCode = (int)upstreamResponse.StatusCode;
            httpContext.Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
            await httpContext.Response.WriteAsync(responseContent, cancellationToken);

            // Cache successful responses in the background
            if (options.EnableCaching && upstreamResponse.IsSuccessStatusCode)
            {
                var curlRequest = await httpContext.Request.ToCurlCommandAsync().ConfigureAwait(false);
                cacheWriter.Enqueue(requestHash, responseContent, curlRequest, EmptyInstructions);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            httpContext.Response.StatusCode = 502;
            httpContext.Response.ContentType = "application/json";
            var error = JsonSerializer.Serialize(new { error = $"Proxy error: {ex.Message}", status = 502 });
            await httpContext.Response.WriteAsync(error, cancellationToken);
        }
    }

    public Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken = default)
        => cacheWriter.DrainAsync(cancellationToken);

    public void Dispose()
    {
        upstreamClient?.Dispose();
    }
}
