namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe charge API endpoints.
/// </summary>
public class StripeChargesBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeChargesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/charges/:id endpoint.
    /// </summary>
    /// <param name="chargeId">The charge ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Get(string chargeId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/charges/{chargeId}", "GET", "charge");
    }

    /// <summary>
    /// Configures the GET /v1/charges endpoint (list charges).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/charges", "GET", "charge_list");
    }

    /// <summary>
    /// Configures the POST /v1/charges endpoint (create charge).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/charges", "POST", "charge");
    }

    /// <summary>
    /// Configures the POST /v1/charges/:id/capture endpoint.
    /// </summary>
    /// <param name="chargeId">The charge ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Capture(string chargeId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/charges/{chargeId}/capture", "POST", "charge");
    }
}
