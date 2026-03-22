using System.Net.Http;

namespace Augustus.AI;
/// <summary>
/// Retries HTTP upstream calls on rate limits and transient server errors, aligned with <see cref="AIOptions"/> backoff settings.
/// </summary>
internal static class HttpUpstreamRetry
{
    public static async Task<HttpResponseMessage> SendWithRetriesAsync(
        Func<HttpRequestMessage> requestFactory,
        HttpClient httpClient,
        AIOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        var delayMs = options.InitialRetryDelayMs;
        for (var attempt = 1; ; attempt++)
        {
            using var request = requestFactory();
            var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var status = (int)response.StatusCode;

            var shouldRetry = status == 429 || (status >= 500 && status <= 504);
            if (!shouldRetry || attempt > options.MaxRetries)
                return response;

            var wait = OpenAIRetryDelays.ComputeProxyRetryDelayMs(options, delayMs, response);
            response.Dispose();
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            delayMs = Math.Min(delayMs * 2, options.MaxRetryDelayMs);
        }
    }
}
