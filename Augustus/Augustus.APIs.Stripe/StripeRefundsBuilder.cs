namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe refund API endpoints.
/// </summary>
public sealed class StripeRefundsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeRefundsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/refunds/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string refundId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/refunds/{refundId}", "GET", "refund");
    }

    /// <summary>
    /// Configures the GET /v1/refunds endpoint (list refunds).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/refunds", "GET", "refund_list");
    }

    /// <summary>
    /// Configures the POST /v1/refunds endpoint (create refund).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/refunds", "POST", "refund");
    }

    /// <summary>
    /// Configures the POST /v1/refunds/:id endpoint (update refund).
    /// </summary>
    public StripeResourceConfigurer Update(string refundId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/refunds/{refundId}", "POST", "refund");
    }
}
