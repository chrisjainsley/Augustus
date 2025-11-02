namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe customer API endpoints.
/// </summary>
public class StripeCustomersBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeCustomersBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/customers/:id endpoint.
    /// </summary>
    /// <param name="customerId">The customer ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Get(string customerId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/customers/{customerId}", "GET", "customer");
    }

    /// <summary>
    /// Configures the GET /v1/customers endpoint (list customers).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/customers", "GET", "customer_list");
    }

    /// <summary>
    /// Configures the POST /v1/customers endpoint (create customer).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/customers", "POST", "customer");
    }

    /// <summary>
    /// Configures the POST /v1/customers/:id endpoint (update customer).
    /// </summary>
    /// <param name="customerId">The customer ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Update(string customerId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/customers/{customerId}", "POST", "customer");
    }

    /// <summary>
    /// Configures the DELETE /v1/customers/:id endpoint.
    /// </summary>
    /// <param name="customerId">The customer ID pattern (use "{id}" for wildcard).</param>
    public StripeResourceConfigurer Delete(string customerId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/customers/{customerId}", "DELETE", "customer_deleted");
    }
}
