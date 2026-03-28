using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Augustus;

/// <summary>
/// Routes incoming HTTP requests to matched route strategies, with optional default handler.
/// </summary>
/// <remarks>
/// Resolution order:
/// 1. Route with <see cref="IResponseStrategy"/> → execute strategy
/// 2. Default handler (if configured) → delegate
/// 3. HTTP 404 JSON error
/// </remarks>
internal class RoutingRequestHandler : IRequestHandler
{
    private readonly APISimulator simulator;

    internal IRequestHandler? DefaultHandler { get; set; }

    public RoutingRequestHandler(APISimulator simulator)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
    }

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        // Record the request before any routing or response generation
        string? body = null;
        if (context.Request.ContentLength is > 0)
        {
            var bodyBytes = await context.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);
            body = Encoding.UTF8.GetString(bodyBytes);
        }

        simulator.RecordRequest(new RecordedRequest
        {
            Method = ParseMethod(method),
            Path = path,
            Headers = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
                context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            Body = body,
            Timestamp = DateTimeOffset.UtcNow
        });

        var route = simulator.GetRouteForRequest(path, method);
        if (route?.ResponseStrategy != null)
        {
            await ApplyLatencyAsync(cancellationToken);
            await route.ResponseStrategy.GenerateResponseAsync(context, cancellationToken);
            return;
        }

        if (DefaultHandler != null)
        {
            await ApplyLatencyAsync(cancellationToken);
            await DefaultHandler.HandleAsync(context, cancellationToken);
            return;
        }

        // No route match and no default handler — return 404
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        var error = JsonSerializer.Serialize(new
        {
            error = $"No route configured for {method} {path}",
            status = 404
        });
        await context.Response.WriteAsync(error, cancellationToken);
    }

    public Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken)
        => DefaultHandler?.DrainPendingCacheWritesAsync(cancellationToken) ?? Task.CompletedTask;

    private Task ApplyLatencyAsync(CancellationToken cancellationToken)
    {
        if (simulator.Latency is { } latency)
        {
            var delayMs = GaussianLatency.Sample(latency.MeanMs, latency.StdDevMs);
            if (delayMs > 0)
                return Task.Delay(delayMs, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private static HttpMethod ParseMethod(string method) => method.ToUpperInvariant() switch
    {
        "GET" => HttpMethod.Get,
        "POST" => HttpMethod.Post,
        "PUT" => HttpMethod.Put,
        "DELETE" => HttpMethod.Delete,
        "PATCH" => HttpMethod.Patch,
        "HEAD" => HttpMethod.Head,
        "OPTIONS" => HttpMethod.Options,
        "TRACE" => HttpMethod.Trace,
        _ => new HttpMethod(method),
    };
}
