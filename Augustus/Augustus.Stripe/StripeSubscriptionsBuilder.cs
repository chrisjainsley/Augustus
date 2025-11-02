namespace Augustus.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe subscription API endpoints.
/// </summary>
public class StripeSubscriptionsBuilder
{
    private readonly MockServer mockServer;

    internal StripeSubscriptionsBuilder(MockServer mockServer)
    {
        this.mockServer = mockServer ?? throw new ArgumentNullException(nameof(mockServer));
    }

    /// <summary>
    /// Configures the GET /v1/subscriptions/:id endpoint.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Get(string subscriptionId = "{id}")
    {
        return new StripeResourceConfigurer(mockServer, $"/v1/subscriptions/{subscriptionId}", "GET", "subscription");
    }

    /// <summary>
    /// Configures the GET /v1/subscriptions endpoint (list subscriptions).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(mockServer, "/v1/subscriptions", "GET", "subscription_list");
    }

    /// <summary>
    /// Configures the POST /v1/subscriptions endpoint (create subscription).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(mockServer, "/v1/subscriptions", "POST", "subscription");
    }

    /// <summary>
    /// Configures the POST /v1/subscriptions/:id endpoint (update subscription).
    /// </summary>
    /// <param name="subscriptionId">The subscription ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Update(string subscriptionId = "{id}")
    {
        return new StripeResourceConfigurer(mockServer, $"/v1/subscriptions/{subscriptionId}", "POST", "subscription");
    }

    /// <summary>
    /// Configures the DELETE /v1/subscriptions/:id endpoint.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Cancel(string subscriptionId = "{id}")
    {
        return new StripeResourceConfigurer(mockServer, $"/v1/subscriptions/{subscriptionId}", "DELETE", "subscription_canceled");
    }
}
