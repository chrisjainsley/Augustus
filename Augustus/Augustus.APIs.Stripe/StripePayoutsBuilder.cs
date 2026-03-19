namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe payout API endpoints.
/// </summary>
public sealed class StripePayoutsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripePayoutsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/payouts/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string payoutId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payouts/{payoutId}", "GET", "payout");
    }

    /// <summary>
    /// Configures the GET /v1/payouts endpoint (list payouts).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/payouts", "GET", "payout_list");
    }

    /// <summary>
    /// Configures the POST /v1/payouts endpoint (create payout).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/payouts", "POST", "payout");
    }

    /// <summary>
    /// Configures the POST /v1/payouts/:id endpoint (update payout).
    /// </summary>
    public StripeResourceConfigurer Update(string payoutId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payouts/{payoutId}", "POST", "payout");
    }

    /// <summary>
    /// Configures the POST /v1/payouts/:id/cancel endpoint.
    /// </summary>
    public StripeResourceConfigurer Cancel(string payoutId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payouts/{payoutId}/cancel", "POST", "payout_canceled");
    }
}
