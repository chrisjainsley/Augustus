namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe coupon API endpoints.
/// </summary>
public class StripeCouponsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeCouponsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/coupons/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string couponId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/coupons/{couponId}", "GET", "coupon");
    }

    /// <summary>
    /// Configures the GET /v1/coupons endpoint (list coupons).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/coupons", "GET", "coupon_list");
    }

    /// <summary>
    /// Configures the POST /v1/coupons endpoint (create coupon).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/coupons", "POST", "coupon");
    }

    /// <summary>
    /// Configures the POST /v1/coupons/:id endpoint (update coupon).
    /// </summary>
    public StripeResourceConfigurer Update(string couponId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/coupons/{couponId}", "POST", "coupon");
    }

    /// <summary>
    /// Configures the DELETE /v1/coupons/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Delete(string couponId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/coupons/{couponId}", "DELETE", "coupon_deleted");
    }
}
