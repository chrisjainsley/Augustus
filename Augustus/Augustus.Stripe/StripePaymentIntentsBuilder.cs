namespace Augustus.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe payment intent API endpoints.
/// </summary>
public class StripePaymentIntentsBuilder
{
    private readonly MockServer mockServer;

    internal StripePaymentIntentsBuilder(MockServer mockServer)
    {
        this.mockServer = mockServer ?? throw new ArgumentNullException(nameof(mockServer));
    }

    /// <summary>
    /// Configures the GET /v1/payment_intents/:id endpoint.
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Get(string paymentIntentId = "{id}")
    {
        return new StripeResourceConfigurer(mockServer, $"/v1/payment_intents/{paymentIntentId}", "GET", "payment_intent");
    }

    /// <summary>
    /// Configures the GET /v1/payment_intents endpoint (list payment intents).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(mockServer, "/v1/payment_intents", "GET", "payment_intent_list");
    }

    /// <summary>
    /// Configures the POST /v1/payment_intents endpoint (create payment intent).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(mockServer, "/v1/payment_intents", "POST", "payment_intent");
    }

    /// <summary>
    /// Configures the POST /v1/payment_intents/:id/confirm endpoint.
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Confirm(string paymentIntentId = "{id}")
    {
        return new StripeResourceConfigurer(mockServer, $"/v1/payment_intents/{paymentIntentId}/confirm", "POST", "payment_intent");
    }

    /// <summary>
    /// Configures the POST /v1/payment_intents/:id/cancel endpoint.
    /// </summary>
    /// <param name="paymentIntentId">The payment intent ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Cancel(string paymentIntentId = "{id}")
    {
        return new StripeResourceConfigurer(mockServer, $"/v1/payment_intents/{paymentIntentId}/cancel", "POST", "payment_intent_canceled");
    }
}
