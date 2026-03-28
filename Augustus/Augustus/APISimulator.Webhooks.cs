namespace Augustus;

/// <summary>
/// Webhook delivery extensions for the API simulator.
/// </summary>
public sealed partial class APISimulator
{
    private WebhookDeliveryService? webhookService;
    private readonly List<WebhookTrigger> webhookTriggers = new();
    private readonly object webhookTriggersLock = new();
    private volatile WebhookTrigger[] webhookTriggersSnapshot = Array.Empty<WebhookTrigger>();

    /// <summary>
    /// Registers a webhook delivery endpoint. Webhook events configured via
    /// <see cref="OnRequest(System.Net.Http.HttpMethod, string)"/> will be POSTed to this URL.
    /// </summary>
    /// <param name="url">The URL to receive webhook events.</param>
    /// <returns>This <see cref="APISimulator"/> instance for method chaining.</returns>
    public APISimulator WithWebhook(string url)
    {
        return WithWebhook(url, configure: null);
    }

    /// <summary>
    /// Registers a webhook delivery endpoint with signing and other options.
    /// </summary>
    /// <param name="url">The URL to receive webhook events.</param>
    /// <param name="configure">Optional action to configure signing and other delivery options.</param>
    /// <returns>This <see cref="APISimulator"/> instance for method chaining.</returns>
    public APISimulator WithWebhook(string url, Action<WebhookDeliveryOptions>? configure)
    {
        if (webhookService != null)
            throw new InvalidOperationException("WithWebhook has already been called. Only one webhook endpoint is supported per simulator.");

        var deliveryOptions = new WebhookDeliveryOptions(url);
        configure?.Invoke(deliveryOptions);
        webhookService = new WebhookDeliveryService(deliveryOptions);
        return this;
    }

    /// <summary>
    /// Begins configuring a webhook trigger for requests matching the specified HTTP method and URL pattern.
    /// </summary>
    /// <param name="httpMethod">The HTTP method to match (e.g., <see cref="System.Net.Http.HttpMethod.Post"/>).</param>
    /// <param name="pattern">The URL pattern to match. Supports {id} for path segments and {*} for wildcards.</param>
    /// <returns>A <see cref="WebhookTriggerBuilder"/> for fluent configuration.</returns>
    public WebhookTriggerBuilder OnRequest(System.Net.Http.HttpMethod httpMethod, string pattern)
    {
        return new WebhookTriggerBuilder(this, pattern, httpMethod.Method);
    }

    /// <summary>
    /// Begins configuring a webhook trigger for requests matching the specified HTTP verb and URL pattern.
    /// </summary>
    /// <param name="httpVerb">The HTTP verb to match.</param>
    /// <param name="pattern">The URL pattern to match.</param>
    /// <returns>A <see cref="WebhookTriggerBuilder"/> for fluent configuration.</returns>
    public WebhookTriggerBuilder OnRequest(HttpVerb httpVerb, string pattern)
    {
        return new WebhookTriggerBuilder(this, pattern, httpVerb.ToMethodString());
    }

    /// <summary>
    /// Gets a read-only collection of all webhooks that have been delivered (or attempted).
    /// Use this in test assertions to verify webhook behavior.
    /// </summary>
    public IReadOnlyCollection<DeliveredWebhook> DeliveredWebhooks =>
        webhookService?.DeliveredWebhooks ?? Array.Empty<DeliveredWebhook>();

    internal void AddWebhookTriggerInternal(WebhookTrigger trigger)
    {
        lock (webhookTriggersLock)
        {
            webhookTriggers.Add(trigger);
            webhookTriggersSnapshot = webhookTriggers.ToArray();
        }
    }

    internal void FireWebhooksForRequest(string path, string method)
    {
        if (webhookService == null)
            return;

        // Read volatile snapshot -- no lock needed on the hot path
        var triggers = webhookTriggersSnapshot;
        foreach (var trigger in triggers)
        {
            if (trigger.Matches(path, method))
                webhookService.EnqueueDelivery(trigger);
        }
    }

    internal Task DrainPendingWebhookDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        return webhookService?.DrainAsync(cancellationToken) ?? Task.CompletedTask;
    }
}
