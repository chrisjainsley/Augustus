namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe dispute API endpoints.
/// </summary>
public sealed class StripeDisputesBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeDisputesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/disputes/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string disputeId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/disputes/{disputeId}", "GET", "dispute");
    }

    /// <summary>
    /// Configures the GET /v1/disputes endpoint (list disputes).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/disputes", "GET", "dispute_list");
    }

    /// <summary>
    /// Configures the POST /v1/disputes/:id endpoint (update dispute).
    /// </summary>
    public StripeResourceConfigurer Update(string disputeId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/disputes/{disputeId}", "POST", "dispute");
    }

    /// <summary>
    /// Configures the POST /v1/disputes/:id/close endpoint.
    /// </summary>
    public StripeResourceConfigurer Close(string disputeId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/disputes/{disputeId}/close", "POST", "dispute_closed");
    }
}
