namespace Augustus.AI;

using Augustus;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

/// <summary>
/// Response strategy that proxies requests to a real API and caches the responses.
/// </summary>
/// <remarks>
/// Per-route dynamic content fields are supplied via the constructor (typically from
/// <see cref="RouteBuilder.WithDynamicFields"/>). Global <see cref="APISimulatorOptions.DynamicContentFields"/>
/// are applied by the default handlers (<see cref="ProxyDefaultHandler"/>, <see cref="AIDefaultHandler"/>)
/// and do not automatically apply to per-route strategies.
/// </remarks>
public sealed class RealApiProxyStrategy : IResponseStrategy, IDisposable
{
    private readonly string baseUrl;
    private readonly bool enableCaching;
    private readonly CacheManager? cacheManager;
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, string> defaultHeaders;
    private readonly IReadOnlyList<string>? dynamicContentFields;

    public RealApiProxyStrategy(string baseUrl, AIOptions? options = null, Dictionary<string, string>? headers = null, IReadOnlyList<string>? dynamicContentFields = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        this.baseUrl = baseUrl.TrimEnd('/');
        this.enableCaching = options?.EnableCaching ?? true;
        this.defaultHeaders = headers ?? new Dictionary<string, string>();
        this.dynamicContentFields = dynamicContentFields;

        httpClient = new HttpClient();

        if (enableCaching && options != null)
        {
            cacheManager = new CacheManager(options.CacheFolderPath);
        }
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = httpContext.Request.Path.Value ?? "/";
            var queryString = httpContext.Request.QueryString.Value ?? "";
            var method = httpContext.Request.Method;

            // Read body for cache key and proxying
            var bodyBytes = await httpContext.Request.ReadBodyBytesAsync(cancellationToken);

            // Generate cache key (includes body for correct POST/PUT/PATCH caching)
            var cacheKey = CacheKeyComputer.ComputeCacheKey(method, path, queryString, bodyBytes, dynamicContentFields: dynamicContentFields);

            // Try cache first
            if (cacheManager != null)
            {
                var cachedResponse = await cacheManager.ReadCachedResponseAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken);
                    return;
                }
            }

            // Proxy to real API
            var realUrl = $"{baseUrl}{path}{queryString}";
            using (var request = new HttpRequestMessage(new HttpMethod(method), realUrl))
            {
                // Copy headers (skip pseudo-headers and Host)
                foreach (var header in httpContext.Request.Headers)
                {
                    if (header.Key.StartsWith(":") || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                        continue;
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                // Add default headers
                foreach (var header in defaultHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Copy body for POST/PUT/PATCH (using already-read bodyBytes)
                if (bodyBytes.Length > 0 &&
                    (method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                     method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                     method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                {
                    request.Content = new ByteArrayContent(bodyBytes);
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        httpContext.Request.ContentType ?? "application/json");
                }

                // Make the request
                using (var response = await httpClient.SendAsync(request, cancellationToken))
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    // Cache if successful
                    if (cacheManager != null && response.IsSuccessStatusCode)
                    {
                        try
                        {
                            await cacheManager.CacheResponseAsync(
                                cacheKey,
                                responseContent,
                                $"{method} {realUrl}",
                                new List<string> { "Real API response" });
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
                        }
                    }

                    // Return response
                    httpContext.Response.StatusCode = (int)response.StatusCode;
                    httpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
                    await httpContext.Response.WriteAsync(responseContent, cancellationToken);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Proxy HTTP error: {ex}");
            await WriteErrorResponse(httpContext, "Failed to proxy request to real API", 502, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Proxy request timeout: {ex}");
            await WriteErrorResponse(httpContext, "Request timeout while proxying to real API", 504, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Proxy request cancelled: {ex}");
            await WriteErrorResponse(httpContext, "Request cancelled", 499, cancellationToken);
        }
    }

    private async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new { error = message ?? "Unknown error", status = statusCode });
        await context.Response.WriteAsync(errorResponse, cancellationToken);
    }

    /// <summary>
    /// Disposes the real API proxy strategy and its resources.
    /// </summary>
    public void Dispose()
    {
        httpClient?.Dispose();
    }
}
