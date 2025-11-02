namespace Augustus.Stripe;

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
    public static StripeMock CreateStripeMock(this object testClass, Action<MockServerOptions>? configureOptions = null)
    {
        var options = new MockServerOptions();
        configureOptions?.Invoke(options);
        var mockServer = new MockServer(options);
        return new StripeMock(mockServer);
    }

    /// <summary>
    /// Accesses Stripe-specific route configuration for an existing mock server.
    /// </summary>
    /// <param name="mockServer">The mock server instance.</param>
    /// <returns>A <see cref="StripeRouteBuilder"/> for configuring Stripe routes.</returns>
    public static StripeRouteBuilder Stripe(this MockServer mockServer)
    {
        return new StripeRouteBuilder(mockServer);
    }
}

/// <summary>
/// Wrapper around MockServer that provides Stripe-specific fluent API at creation time.
/// </summary>
public class StripeMock
{
    private readonly MockServer mockServer;

    internal StripeMock(MockServer mockServer)
    {
        this.mockServer = mockServer ?? throw new ArgumentNullException(nameof(mockServer));
    }

    /// <summary>
    /// Gets the underlying MockServer instance.
    /// </summary>
    public MockServer MockServer => mockServer;

    /// <summary>
    /// Configures Stripe customer endpoints.
    /// </summary>
    public StripeCustomersBuilder Customers() => new(mockServer);

    /// <summary>
    /// Configures Stripe charge endpoints.
    /// </summary>
    public StripeChargesBuilder Charges() => new(mockServer);

    /// <summary>
    /// Configures Stripe payment intent endpoints.
    /// </summary>
    public StripePaymentIntentsBuilder PaymentIntents() => new(mockServer);

    /// <summary>
    /// Configures Stripe subscription endpoints.
    /// </summary>
    public StripeSubscriptionsBuilder Subscriptions() => new(mockServer);

    /// <summary>
    /// Builds the mock server and returns it for starting.
    /// </summary>
    public MockServer Build() => mockServer;

    // Delegate common MockServer operations
    public Task StartAsync(CancellationToken cancellationToken = default) => mockServer.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => mockServer.StopAsync(cancellationToken);
    public HttpClient CreateClient() => mockServer.CreateClient();
    public bool IsRunning => mockServer.IsRunning;
}

/// <summary>
/// Provides access to Stripe route builders from an existing MockServer.
/// </summary>
public class StripeRouteBuilder
{
    private readonly MockServer mockServer;

    internal StripeRouteBuilder(MockServer mockServer)
    {
        this.mockServer = mockServer ?? throw new ArgumentNullException(nameof(mockServer));
    }

    /// <summary>
    /// Configures Stripe customer endpoints.
    /// </summary>
    public StripeCustomersBuilder Customers() => new(mockServer);

    /// <summary>
    /// Configures Stripe charge endpoints.
    /// </summary>
    public StripeChargesBuilder Charges() => new(mockServer);

    /// <summary>
    /// Configures Stripe payment intent endpoints.
    /// </summary>
    public StripePaymentIntentsBuilder PaymentIntents() => new(mockServer);

    /// <summary>
    /// Configures Stripe subscription endpoints.
    /// </summary>
    public StripeSubscriptionsBuilder Subscriptions() => new(mockServer);
}
