using Augustus;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;

namespace Augustus.AI;

/// <summary>
/// An <see cref="IRequestHandler"/> that proxies requests to a real upstream API and caches responses.
/// Installed as the default handler on the simulator's routing pipeline.
/// </summary>
internal class ProxyDefaultHandler : IRequestHandler, IDisposable
{
    private static readonly HashSet<string> SkippedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Connection", "Keep-Alive", "Transfer-Encoding",
        "TE", "Trailer", "Upgrade", "Proxy-Authorization", "Proxy-Authenticate",
        "Content-Type", "Content-Length"
    };

    private static readonly HashSet<string> AuthHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "api-key"
    };

    private readonly AIOptions aiOptions;
    private readonly APISimulator simulator;
    private readonly string upstreamEndpoint;
    private readonly HttpClient? httpClient;
    private readonly BackgroundCacheWriter _cacheWriter = new();

    public ProxyDefaultHandler(APISimulator simulator, AIOptions aiOptions, string upstreamEndpoint)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        this.aiOptions = aiOptions ?? throw new ArgumentNullException(nameof(aiOptions));
        this.upstreamEndpoint = upstreamEndpoint ?? throw new ArgumentNullException(nameof(upstreamEndpoint));

        if (!simulator.Options.CacheOnly)
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(upstreamEndpoint),
                Timeout = TimeSpan.FromSeconds(aiOptions.ProxyTimeoutSeconds)
            };
        }
    }

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var fileManager = simulator.CacheFileManager;
        var options = simulator.Options;

        try
        {
            var bodyBytes = await context.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);
            var cacheKey = CacheKeyComputer.ComputeCacheKey(
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Request.QueryString.Value,
                bodyBytes,
                out var materializedBody,
                dynamicContentFields: options.DynamicContentFields);

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

            if (options.CacheOnly)
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/json";
                var payload = new Dictionary<string, object?>
                {
                    ["error"] = "Cache miss in CacheOnly mode. No cached response found for this request.",
                    ["status"] = 503,
                    ["requestHash"] = cacheKey
                };
                var digestInputLen = aiOptions.CacheMissMaterializedBodyPrefixSha256ByteCount;
                if (digestInputLen > 0)
                {
                    var n = Math.Min(digestInputLen, materializedBody.Length);
                    if (n > 0)
                    {
                        var digest = SHA256.HashData(materializedBody.AsSpan(0, n));
                        payload["materializedBodyPrefixSha256"] = Convert.ToHexString(digest);
                    }
                }

                var error = JsonSerializer.Serialize(payload);
                await context.Response.WriteAsync(error, cancellationToken).ConfigureAwait(false);
                return;
            }

            using var upstreamResponse = await HttpUpstreamRetry.SendWithRetriesAsync(
                    () => BuildUpstreamRequest(context.Request, bodyBytes),
                    httpClient!,
                    aiOptions,
                    cancellationToken)
                .ConfigureAwait(false);

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

        var hasConfiguredKey = !string.IsNullOrEmpty(aiOptions.OpenAIApiKey);

        foreach (var header in incoming.Headers)
        {
            if (SkippedHeaders.Contains(header.Key))
                continue;

            // When a key is configured, the proxy replaces auth headers below.
            // When no key is configured, forward the caller's auth headers as-is.
            if (hasConfiguredKey && AuthHeaders.Contains(header.Key))
                continue;

            request.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }

        if (hasConfiguredKey)
        {
            if (aiOptions.UseAzureOpenAI)
            {
                request.Headers.TryAddWithoutValidation("api-key", aiOptions.OpenAIApiKey);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {aiOptions.OpenAIApiKey}");
            }
        }

        return request;
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
