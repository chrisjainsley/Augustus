using System.ClientModel;
using System.Globalization;
using System.Net.Http;

namespace Augustus.AI;

/// <summary>
/// Computes retry delays with optional <c>Retry-After</c> header support and jitter.
/// </summary>
internal static class OpenAIRetryDelays
{
    /// <summary>
    /// Returns milliseconds to wait before the next retry, capped by <paramref name="maxRetryDelayMs"/>.
    /// </summary>
    public static int ComputeNextDelayMs(
        AIOptions options,
        int currentBackoffMs,
        ClientResultException? clientException)
    {
        var retryAfterMs = TryGetRetryAfterMilliseconds(clientException);
        var baseDelay = Math.Max(currentBackoffMs, retryAfterMs ?? 0);
        if (baseDelay <= 0)
            baseDelay = options.InitialRetryDelayMs;

        var jitterCap = Math.Max(1, Math.Min(750, baseDelay / 8 + 50));
        var jitter = Random.Shared.Next(0, jitterCap);
        var total = Math.Min(baseDelay + jitter, options.MaxRetryDelayMs);
        return total;
    }

    public static int? TryGetRetryAfterMilliseconds(ClientResultException? ex)
    {
        if (ex == null)
            return null;

        try
        {
            var raw = ex.GetRawResponse();
            if (raw?.Headers is null)
                return null;

            if (!raw.Headers.TryGetValues("Retry-After", out var values))
                return null;

            var value = values?.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
                return Math.Max(0, seconds) * 1000;

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var until))
            {
                var ms = (int)(until - DateTimeOffset.UtcNow).TotalMilliseconds;
                return ms > 0 ? ms : 0;
            }
        }
        catch
        {
            // ignore header parse failures
        }

        return null;
    }

    /// <summary>
    /// Parses <c>Retry-After</c> from an <see cref="HttpResponseMessage"/> (proxy / real API paths).
    /// </summary>
    public static int? TryGetRetryAfterMilliseconds(HttpResponseMessage? response)
    {
        if (response?.Headers?.RetryAfter is null)
            return null;

        if (response.Headers.RetryAfter.Delta is { } delta)
            return (int)Math.Max(0, delta.TotalMilliseconds);

        if (response.Headers.RetryAfter.Date is { } date)
        {
            var ms = (int)(date - DateTimeOffset.UtcNow).TotalMilliseconds;
            return ms > 0 ? ms : 0;
        }

        return null;
    }

    public static int ComputeProxyRetryDelayMs(AIOptions options, int currentBackoffMs, HttpResponseMessage? response)
    {
        var retryAfterMs = TryGetRetryAfterMilliseconds(response);
        var baseDelay = Math.Max(currentBackoffMs, retryAfterMs ?? 0);
        if (baseDelay <= 0)
            baseDelay = options.InitialRetryDelayMs;

        var jitterCap = Math.Max(1, Math.Min(750, baseDelay / 8 + 50));
        var jitter = Random.Shared.Next(0, jitterCap);
        return Math.Min(baseDelay + jitter, options.MaxRetryDelayMs);
    }
}
