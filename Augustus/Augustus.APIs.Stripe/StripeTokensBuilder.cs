namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe token API endpoints.
/// </summary>
public class StripeTokensBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeTokensBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/tokens/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string tokenId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/tokens/{tokenId}", "GET", "token");
    }

    /// <summary>
    /// Configures the POST /v1/tokens endpoint (create token).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/tokens", "POST", "token");
    }
}
