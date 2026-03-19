namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe event API endpoints.
/// </summary>
public class StripeEventsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeEventsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/events/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string eventId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/events/{eventId}", "GET", "event");
    }

    /// <summary>
    /// Configures the GET /v1/events endpoint (list events).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/events", "GET", "event_list");
    }
}
