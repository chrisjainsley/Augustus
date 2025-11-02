namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Extension methods for adding Stripe-specific mocking capabilities to Augustus.
/// </summary>
public static class StripeExtensions
{
    /// <summary>
    /// Creates a Stripe mock server with pre-configured Stripe API structure.
    /// </summary>
    /// <param name="testClass">The test class instance (typically 'this').</param>
    /// <param name="configureOptions">Optional action to configure server options.</param>
    /// <returns>A new <see cref="StripeMock"/> instance.</returns>
    public static StripeMock CreateStripeMock(this object testClass, Action<APISimulatorOptions>? configureOptions = null)
    {
        var options = new APISimulatorOptions();
        configureOptions?.Invoke(options);
        var apiSimulator = new APISimulator(options);
        return new StripeMock(apiSimulator);
    }

    /// <summary>
    /// Accesses Stripe-specific route configuration for an existing mock server.
    /// </summary>
    /// <param name="apiSimulator">The mock server instance.</param>
    /// <returns>A <see cref="StripeRouteBuilder"/> for configuring Stripe routes.</returns>
    public static StripeRouteBuilder Stripe(this APISimulator apiSimulator)
    {
        return new StripeRouteBuilder(apiSimulator);
    }
}

/// <summary>
/// Wrapper around APISimulator that provides Stripe-specific fluent API at creation time.
/// </summary>
public class StripeMock
{
    private readonly APISimulator apiSimulator;

    internal StripeMock(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Gets the underlying APISimulator instance.
    /// </summary>
    public APISimulator APISimulator => apiSimulator;

    /// <summary>
    /// Configures Stripe customer endpoints.
    /// </summary>
    public StripeCustomersBuilder Customers() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe charge endpoints.
    /// </summary>
    public StripeChargesBuilder Charges() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe payment intent endpoints.
    /// </summary>
    public StripePaymentIntentsBuilder PaymentIntents() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe subscription endpoints.
    /// </summary>
    public StripeSubscriptionsBuilder Subscriptions() => new(apiSimulator);

    /// <summary>
    /// Builds the mock server and returns it for starting.
    /// </summary>
    public APISimulator Build() => apiSimulator;

    // Delegate common APISimulator operations
    public Task StartAsync(CancellationToken cancellationToken = default) => apiSimulator.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => apiSimulator.StopAsync(cancellationToken);
    public HttpClient CreateClient() => apiSimulator.CreateClient();
    public bool IsRunning => apiSimulator.IsRunning;
}

/// <summary>
/// Provides access to Stripe route builders from an existing APISimulator.
/// </summary>
public class StripeRouteBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeRouteBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures Stripe customer endpoints.
    /// </summary>
    public StripeCustomersBuilder Customers() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe charge endpoints.
    /// </summary>
    public StripeChargesBuilder Charges() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe payment intent endpoints.
    /// </summary>
    public StripePaymentIntentsBuilder PaymentIntents() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe subscription endpoints.
    /// </summary>
    public StripeSubscriptionsBuilder Subscriptions() => new(apiSimulator);
}
