namespace Augustus;

using System.Text.Json;

/// <summary>
/// Fluent builder for configuring webhook triggers on matched requests.
/// </summary>
/// <remarks>
/// Created via <see cref="APISimulator.OnRequest(System.Net.Http.HttpMethod, string)"/>.
/// Use <see cref="FireWebhookEvent"/> to specify which webhook event to fire,
/// then call <see cref="Add"/> to register the trigger.
/// </remarks>
/// <example>
/// <code>
/// simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
///     .FireWebhookEvent("customer.subscription.created")
///     .WithPayload(new { type = "customer.subscription.created", data = new { id = "sub_123" } })
///     .WithDelay(TimeSpan.FromMilliseconds(200))
///     .Add();
/// </code>
/// </example>
public sealed class WebhookTriggerBuilder
{
    private readonly APISimulator simulator;
    private readonly string pattern;
    private readonly string httpMethod;
    private string? eventType;
    private string? staticPayload;
    private Func<string>? payloadFactory;
    private TimeSpan delay = TimeSpan.Zero;

    internal WebhookTriggerBuilder(APISimulator simulator, string pattern, string httpMethod)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this.httpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
    }

    /// <summary>
    /// Specifies the webhook event type to fire when the request matches.
    /// </summary>
    /// <param name="eventType">The event type string (e.g., "customer.subscription.created").</param>
    /// <returns>This builder for method chaining.</returns>
    public WebhookTriggerBuilder FireWebhookEvent(string eventType)
    {
        this.eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        return this;
    }

    /// <summary>
    /// Sets a static JSON string payload for the webhook event.
    /// </summary>
    /// <param name="jsonPayload">The JSON payload to send.</param>
    /// <returns>This builder for method chaining.</returns>
    public WebhookTriggerBuilder WithPayload(string jsonPayload)
    {
        staticPayload = jsonPayload ?? throw new ArgumentNullException(nameof(jsonPayload));
        payloadFactory = null;
        return this;
    }

    /// <summary>
    /// Sets a payload object that will be serialized to JSON for the webhook event.
    /// </summary>
    /// <param name="payloadObject">The object to serialize.</param>
    /// <returns>This builder for method chaining.</returns>
    public WebhookTriggerBuilder WithPayload(object payloadObject)
    {
        ArgumentNullException.ThrowIfNull(payloadObject);
        staticPayload = JsonSerializer.Serialize(payloadObject);
        payloadFactory = null;
        return this;
    }

    /// <summary>
    /// Sets a factory function that produces the webhook payload at delivery time.
    /// </summary>
    /// <param name="factory">A function returning the JSON payload string.</param>
    /// <returns>This builder for method chaining.</returns>
    public WebhookTriggerBuilder WithPayloadFactory(Func<string> factory)
    {
        payloadFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        staticPayload = null;
        return this;
    }

    /// <summary>
    /// Adds a delay before the webhook is delivered, simulating the async nature of real webhook delivery.
    /// The delay does not block the API response.
    /// </summary>
    /// <param name="delay">The delay before delivery.</param>
    /// <returns>This builder for method chaining.</returns>
    public WebhookTriggerBuilder WithDelay(TimeSpan delay)
    {
        this.delay = delay;
        return this;
    }

    /// <summary>
    /// Registers this webhook trigger with the simulator and returns the simulator for further configuration.
    /// </summary>
    /// <returns>The <see cref="APISimulator"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="FireWebhookEvent"/> has not been called.
    /// </exception>
    public APISimulator Add()
    {
        if (string.IsNullOrEmpty(eventType))
            throw new InvalidOperationException("FireWebhookEvent must be called before Add.");

        var trigger = new WebhookTrigger(pattern, httpMethod, eventType)
        {
            StaticPayload = staticPayload,
            PayloadFactory = payloadFactory,
            Delay = delay,
        };

        simulator.AddWebhookTriggerInternal(trigger);
        return simulator;
    }
}
