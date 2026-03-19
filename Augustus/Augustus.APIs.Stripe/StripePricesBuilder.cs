namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe price API endpoints.
/// </summary>
public sealed class StripePricesBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripePricesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/prices/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string priceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/prices/{priceId}", "GET", "price");
    }

    /// <summary>
    /// Configures the GET /v1/prices endpoint (list prices).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/prices", "GET", "price_list");
    }

    /// <summary>
    /// Configures the POST /v1/prices endpoint (create price).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/prices", "POST", "price");
    }

    /// <summary>
    /// Configures the POST /v1/prices/:id endpoint (update price).
    /// </summary>
    public StripeResourceConfigurer Update(string priceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/prices/{priceId}", "POST", "price");
    }
}
