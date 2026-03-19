namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe setup intent API endpoints.
/// </summary>
public sealed class StripeSetupIntentsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeSetupIntentsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/setup_intents/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string setupIntentId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/setup_intents/{setupIntentId}", "GET", "setup_intent");
    }

    /// <summary>
    /// Configures the GET /v1/setup_intents endpoint (list setup intents).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/setup_intents", "GET", "setup_intent_list");
    }

    /// <summary>
    /// Configures the POST /v1/setup_intents endpoint (create setup intent).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/setup_intents", "POST", "setup_intent");
    }

    /// <summary>
    /// Configures the POST /v1/setup_intents/:id/confirm endpoint.
    /// </summary>
    public StripeResourceConfigurer Confirm(string setupIntentId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/setup_intents/{setupIntentId}/confirm", "POST", "setup_intent");
    }

    /// <summary>
    /// Configures the POST /v1/setup_intents/:id/cancel endpoint.
    /// </summary>
    public StripeResourceConfigurer Cancel(string setupIntentId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/setup_intents/{setupIntentId}/cancel", "POST", "setup_intent_canceled");
    }
}
