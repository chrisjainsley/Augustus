using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Augustus;

internal class ProxyResponseGenerator : IRequestHandler, IDisposable
{
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Connection", "Keep-Alive", "Transfer-Encoding",
        "TE", "Trailer", "Upgrade", "Proxy-Authorization", "Proxy-Authenticate"
    };

    private readonly APISimulatorOptions options;
    private readonly APISimulator.FileManager fileManager;
    private readonly HttpClient? httpClient;
    private readonly BackgroundCacheWriter _cacheWriter = new();

    public ProxyResponseGenerator(APISimulatorOptions options, APISimulator.FileManager fileManager)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));

        if (!options.CacheOnly)
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.ProxyUpstreamEndpoint),
                Timeout = TimeSpan.FromSeconds(options.ProxyTimeoutSeconds)
            };
        }
    }

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            var bodyBytes = await context.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);
            var cacheKey = CacheKeyComputer.ComputeCacheKey(context.Request.Method, context.Request.Path.Value ?? "/", context.Request.QueryString.Value, bodyBytes);

            if (options.EnableCaching)
            {
                var cached = await fileManager.ReadCachedResponseAsync(cacheKey).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(cached))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(cached, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            // Cache miss in CacheOnly mode -> 503
            if (options.CacheOnly)
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/json";
                var error = JsonSerializer.Serialize(new
                {
                    error = "Cache miss in CacheOnly mode. No cached response found for this request.",
                    status = 503,
                    requestHash = cacheKey
                });
                await context.Response.WriteAsync(error, cancellationToken).ConfigureAwait(false);
                return;
            }

            using var upstreamRequest = BuildUpstreamRequest(context.Request, bodyBytes);
            using var upstreamResponse = await httpClient!.SendAsync(upstreamRequest, cancellationToken).ConfigureAwait(false);

            var responseBody = await upstreamResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            context.Response.StatusCode = (int)upstreamResponse.StatusCode;
            context.Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
            await context.Response.WriteAsync(responseBody, cancellationToken).ConfigureAwait(false);

            if (options.EnableCaching && upstreamResponse.IsSuccessStatusCode)
            {
                var method = context.Request.Method;
                var path = context.Request.Path.Value ?? "/";
                _cacheWriter.Enqueue(() =>
                {
                    var requestInfo = $"PROXY {method} {path}";
                    return fileManager.CacheResponseAsync(cacheKey, responseBody, requestInfo, new List<string>());
                });
            }
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 502;
                context.Response.ContentType = "application/json";
                var errorResponse = JsonSerializer.Serialize(new { error = $"Proxy error: {ex.Message}", status = 502 });
                await context.Response.WriteAsync(errorResponse, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken = default)
        => _cacheWriter.DrainAsync(cancellationToken);

    private HttpRequestMessage BuildUpstreamRequest(HttpRequest incoming, byte[] bodyBytes)
    {
        var uri = $"{incoming.Path}{incoming.QueryString}";
        var request = new HttpRequestMessage(new HttpMethod(incoming.Method), uri);

        if (bodyBytes.Length > 0)
        {
            request.Content = new ByteArrayContent(bodyBytes);
            if (incoming.ContentType != null)
            {
                request.Content.Headers.TryAddWithoutValidation("Content-Type", incoming.ContentType);
            }
        }

        foreach (var header in incoming.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;
            if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                continue;
            if (header.Key.Equals("api-key", StringComparison.OrdinalIgnoreCase))
                continue;

            request.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }

        // Set auth from configured credentials (never forward the client's original auth)
        if (options.UseAzureOpenAI)
        {
            request.Headers.TryAddWithoutValidation("api-key", options.OpenAIApiKey);
        }
        else
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {options.OpenAIApiKey}");
        }

        return request;
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
