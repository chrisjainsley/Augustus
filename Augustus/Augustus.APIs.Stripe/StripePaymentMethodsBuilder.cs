namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe payment method API endpoints.
/// </summary>
public class StripePaymentMethodsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripePaymentMethodsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/payment_methods/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string paymentMethodId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payment_methods/{paymentMethodId}", "GET", "payment_method");
    }

    /// <summary>
    /// Configures the GET /v1/payment_methods endpoint (list payment methods).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/payment_methods", "GET", "payment_method_list");
    }

    /// <summary>
    /// Configures the POST /v1/payment_methods endpoint (create payment method).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/payment_methods", "POST", "payment_method");
    }

    /// <summary>
    /// Configures the POST /v1/payment_methods/:id endpoint (update payment method).
    /// </summary>
    public StripeResourceConfigurer Update(string paymentMethodId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payment_methods/{paymentMethodId}", "POST", "payment_method");
    }

    /// <summary>
    /// Configures the POST /v1/payment_methods/:id/attach endpoint.
    /// </summary>
    public StripeResourceConfigurer Attach(string paymentMethodId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payment_methods/{paymentMethodId}/attach", "POST", "payment_method");
    }

    /// <summary>
    /// Configures the POST /v1/payment_methods/:id/detach endpoint.
    /// </summary>
    public StripeResourceConfigurer Detach(string paymentMethodId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/payment_methods/{paymentMethodId}/detach", "POST", "payment_method");
    }
}
