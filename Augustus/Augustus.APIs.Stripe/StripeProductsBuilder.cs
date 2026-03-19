namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe product API endpoints.
/// </summary>
public class StripeProductsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeProductsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/products/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string productId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/products/{productId}", "GET", "product");
    }

    /// <summary>
    /// Configures the GET /v1/products endpoint (list products).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/products", "GET", "product_list");
    }

    /// <summary>
    /// Configures the POST /v1/products endpoint (create product).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/products", "POST", "product");
    }

    /// <summary>
    /// Configures the POST /v1/products/:id endpoint (update product).
    /// </summary>
    public StripeResourceConfigurer Update(string productId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/products/{productId}", "POST", "product");
    }

    /// <summary>
    /// Configures the DELETE /v1/products/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Delete(string productId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/products/{productId}", "DELETE", "product_deleted");
    }
}
