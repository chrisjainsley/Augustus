namespace Augustus.AI;

using Augustus;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

/// <summary>
/// Response strategy that proxies requests to a real API and caches the responses.
/// </summary>
public class RealApiProxyStrategy : IResponseStrategy, IDisposable
{
    private readonly string baseUrl;
    private readonly bool enableCaching;
    private readonly CacheManager? cacheManager;
    private readonly HttpClient httpClient;
    private readonly Dictionary<string, string> defaultHeaders;

    public RealApiProxyStrategy(string baseUrl, AIOptions? options = null, Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        this.baseUrl = baseUrl.TrimEnd('/');
        this.enableCaching = options?.EnableCaching ?? true;
        this.defaultHeaders = headers ?? new Dictionary<string, string>();

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

            // Generate cache key
            var cacheKey = GenerateCacheKey(method, path, queryString);

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
                // Copy headers
                foreach (var header in httpContext.Request.Headers)
                {
                    if (!header.Key.StartsWith(":") && !header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // Add default headers
                foreach (var header in defaultHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Copy body for POST/PUT/PATCH
                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                    method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                    method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
                {
                    if (httpContext.Request.Body.CanSeek)
                    {
                        httpContext.Request.Body.Position = 0;
                    }

                    var bodyContent = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
                    if (!string.IsNullOrEmpty(bodyContent))
                    {
                        request.Content = new StringContent(bodyContent, System.Text.Encoding.UTF8,
                            httpContext.Request.ContentType ?? "application/json");
                    }
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
                        catch (Exception ex)
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Proxy error: {ex}");
            await WriteErrorResponse(httpContext, "Failed to proxy request to real API", 502, cancellationToken);
        }
    }

    private string GenerateCacheKey(string method, string path, string queryString)
    {
        var combined = $"{method}|{path}|{queryString}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash);
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
