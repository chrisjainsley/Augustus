using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Augustus;

/// <summary>
/// Handles fire-and-forget HTTP delivery of webhook events, with optional HMAC signing.
/// Follows the <see cref="BackgroundCacheWriter"/> pattern for async task tracking.
/// </summary>
internal sealed class WebhookDeliveryService
{
    // Shared HttpClient avoids socket exhaustion across service instances.
    private static readonly HttpClient SharedHttpClient = new();

    private readonly WebhookDeliveryOptions options;
    private readonly byte[]? signingKeyBytes;
    private readonly ConcurrentQueue<DeliveredWebhook> deliveredWebhooks = new();
    private readonly ConcurrentDictionary<int, Task> pendingDeliveries = new();
    private CancellationTokenSource? drainCts;
    private int deliveryId;

    public IReadOnlyCollection<DeliveredWebhook> DeliveredWebhooks => deliveredWebhooks.ToArray();

    public WebhookDeliveryService(WebhookDeliveryOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        if (!string.IsNullOrEmpty(options.SigningSecret))
            signingKeyBytes = Encoding.UTF8.GetBytes(options.SigningSecret);
    }

    public void EnqueueDelivery(WebhookTrigger trigger)
    {
        var id = Interlocked.Increment(ref deliveryId);
        // Register the id before starting the task to avoid a race where the task
        // completes (and tries TryRemove) before TryAdd runs.
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pendingDeliveries.TryAdd(id, tcs.Task);

        _ = DeliverAsync(id, trigger, tcs);
    }

    private async Task DeliverAsync(int id, WebhookTrigger trigger, TaskCompletionSource tcs)
    {
        var payload = string.Empty;
        try
        {
            var ct = drainCts?.Token ?? CancellationToken.None;

            if (trigger.Delay > TimeSpan.Zero)
                await Task.Delay(trigger.Delay, ct).ConfigureAwait(false);

            payload = ResolvePayload(trigger);

            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, options.Url);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            if (signingKeyBytes != null)
            {
                string headerValue;
                if (options.WebhookSigner != null)
                {
                    // Custom signer controls the full signing process (e.g., Stripe's t=...,v1=... format)
                    headerValue = options.WebhookSigner(payload, options.SigningSecret!);
                }
                else
                {
                    headerValue = ComputeHmac(payload, signingKeyBytes, options.SignatureAlgorithm);
                }
                request.Headers.TryAddWithoutValidation(options.SignatureHeader, headerValue);
            }

            using var response = await SharedHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            deliveredWebhooks.Enqueue(new DeliveredWebhook(
                trigger.EventType,
                payload,
                DateTimeOffset.UtcNow,
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                null));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Webhook delivery failed for '{trigger.EventType}': {ex.Message}");

            deliveredWebhooks.Enqueue(new DeliveredWebhook(
                trigger.EventType,
                payload,
                DateTimeOffset.UtcNow,
                false,
                null,
                ex.ToString()));
        }
        finally
        {
            pendingDeliveries.TryRemove(id, out _);
            tcs.TrySetResult();
        }
    }

    private static string ResolvePayload(WebhookTrigger trigger)
    {
        if (trigger.PayloadFactory != null)
            return trigger.PayloadFactory();

        if (trigger.StaticPayload != null)
            return trigger.StaticPayload;

        // Default minimal payload with event type
        return System.Text.Json.JsonSerializer.Serialize(new { type = trigger.EventType });
    }

    private static string ComputeHmac(string payload, byte[] keyBytes, WebhookSignatureAlgorithm algorithm)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        byte[] hash;
        switch (algorithm)
        {
            case WebhookSignatureAlgorithm.HmacSha512:
                using (var hmac = new HMACSHA512(keyBytes))
                    hash = hmac.ComputeHash(payloadBytes);
                break;
            default: // HmacSha256
                using (var hmac = new HMACSHA256(keyBytes))
                    hash = hmac.ComputeHash(payloadBytes);
                break;
        }

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        // Signal all in-flight deliveries to cancel via the shared drain token
        drainCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var tasks = pendingDeliveries.Values.ToArray();
        if (tasks.Length > 0)
        {
            var allTasks = Task.WhenAll(tasks);
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            await Task.WhenAny(allTasks, cancellationTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
