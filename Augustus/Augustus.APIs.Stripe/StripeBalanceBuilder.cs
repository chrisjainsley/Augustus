namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe balance API endpoint.
/// </summary>
public class StripeBalanceBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeBalanceBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/balance endpoint (singleton).
    /// </summary>
    public StripeResourceConfigurer Get()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/balance", "GET", "balance");
    }
}
