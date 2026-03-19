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

    /// <summary>
    /// Accesses Stripe-specific route configuration for a StripeMock instance.
    /// </summary>
    /// <param name="stripeMock">The Stripe mock instance.</param>
    /// <returns>A <see cref="StripeRouteBuilder"/> for configuring Stripe routes.</returns>
    public static StripeRouteBuilder Stripe(this StripeMock stripeMock)
    {
        return new StripeRouteBuilder(stripeMock.APISimulator);
    }
}

/// <summary>
/// Wrapper around APISimulator that provides Stripe-specific fluent API at creation time.
/// </summary>
public sealed class StripeMock
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
    /// Configures Stripe payment method endpoints.
    /// </summary>
    public StripePaymentMethodsBuilder PaymentMethods() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe setup intent endpoints.
    /// </summary>
    public StripeSetupIntentsBuilder SetupIntents() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe refund endpoints.
    /// </summary>
    public StripeRefundsBuilder Refunds() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe invoice endpoints.
    /// </summary>
    public StripeInvoicesBuilder Invoices() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe invoice item endpoints.
    /// </summary>
    public StripeInvoiceItemsBuilder InvoiceItems() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe product endpoints.
    /// </summary>
    public StripeProductsBuilder Products() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe price endpoints.
    /// </summary>
    public StripePricesBuilder Prices() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe coupon endpoints.
    /// </summary>
    public StripeCouponsBuilder Coupons() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe balance endpoint.
    /// </summary>
    public StripeBalanceBuilder Balance() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe balance transaction endpoints.
    /// </summary>
    public StripeBalanceTransactionsBuilder BalanceTransactions() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe dispute endpoints.
    /// </summary>
    public StripeDisputesBuilder Disputes() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe payout endpoints.
    /// </summary>
    public StripePayoutsBuilder Payouts() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe event endpoints.
    /// </summary>
    public StripeEventsBuilder Events() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe token endpoints.
    /// </summary>
    public StripeTokensBuilder Tokens() => new(apiSimulator);

    /// <summary>
    /// Registers all endpoints with default responses in one call.
    /// </summary>
    public StripeMock UseAllDefaults()
    {
        // Customers
        Customers().Get().UseDefault();
        Customers().List().UseDefault();
        Customers().Create().UseDefault();
        Customers().Update().UseDefault();
        Customers().Delete().UseDefault();

        // Charges
        Charges().Get().UseDefault();
        Charges().List().UseDefault();
        Charges().Create().UseDefault();
        Charges().Capture().UseDefault();

        // Payment Intents
        PaymentIntents().Get().UseDefault();
        PaymentIntents().List().UseDefault();
        PaymentIntents().Create().UseDefault();
        PaymentIntents().Confirm().UseDefault();
        PaymentIntents().Cancel().UseDefault();

        // Subscriptions
        Subscriptions().Get().UseDefault();
        Subscriptions().List().UseDefault();
        Subscriptions().Create().UseDefault();
        Subscriptions().Update().UseDefault();
        Subscriptions().Cancel().UseDefault();

        // Payment Methods
        PaymentMethods().Get().UseDefault();
        PaymentMethods().List().UseDefault();
        PaymentMethods().Create().UseDefault();
        PaymentMethods().Update().UseDefault();
        PaymentMethods().Attach().UseDefault();
        PaymentMethods().Detach().UseDefault();

        // Setup Intents
        SetupIntents().Get().UseDefault();
        SetupIntents().List().UseDefault();
        SetupIntents().Create().UseDefault();
        SetupIntents().Confirm().UseDefault();
        SetupIntents().Cancel().UseDefault();

        // Refunds
        Refunds().Get().UseDefault();
        Refunds().List().UseDefault();
        Refunds().Create().UseDefault();
        Refunds().Update().UseDefault();

        // Invoices
        Invoices().Get().UseDefault();
        Invoices().List().UseDefault();
        Invoices().Create().UseDefault();
        Invoices().Update().UseDefault();
        Invoices().Delete().UseDefault();
        Invoices().Finalize().UseDefault();
        Invoices().Pay().UseDefault();
        Invoices().Void().UseDefault();

        // Invoice Items
        InvoiceItems().Get().UseDefault();
        InvoiceItems().List().UseDefault();
        InvoiceItems().Create().UseDefault();
        InvoiceItems().Update().UseDefault();
        InvoiceItems().Delete().UseDefault();

        // Products
        Products().Get().UseDefault();
        Products().List().UseDefault();
        Products().Create().UseDefault();
        Products().Update().UseDefault();
        Products().Delete().UseDefault();

        // Prices
        Prices().Get().UseDefault();
        Prices().List().UseDefault();
        Prices().Create().UseDefault();
        Prices().Update().UseDefault();

        // Coupons
        Coupons().Get().UseDefault();
        Coupons().List().UseDefault();
        Coupons().Create().UseDefault();
        Coupons().Update().UseDefault();
        Coupons().Delete().UseDefault();

        // Balance
        Balance().Get().UseDefault();

        // Balance Transactions
        BalanceTransactions().Get().UseDefault();
        BalanceTransactions().List().UseDefault();

        // Disputes
        Disputes().Get().UseDefault();
        Disputes().List().UseDefault();
        Disputes().Update().UseDefault();
        Disputes().Close().UseDefault();

        // Payouts
        Payouts().Get().UseDefault();
        Payouts().List().UseDefault();
        Payouts().Create().UseDefault();
        Payouts().Update().UseDefault();
        Payouts().Cancel().UseDefault();

        // Events
        Events().Get().UseDefault();
        Events().List().UseDefault();

        // Tokens
        Tokens().Get().UseDefault();
        Tokens().Create().UseDefault();

        return this;
    }

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
public sealed class StripeRouteBuilder
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

    /// <summary>
    /// Configures Stripe payment method endpoints.
    /// </summary>
    public StripePaymentMethodsBuilder PaymentMethods() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe setup intent endpoints.
    /// </summary>
    public StripeSetupIntentsBuilder SetupIntents() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe refund endpoints.
    /// </summary>
    public StripeRefundsBuilder Refunds() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe invoice endpoints.
    /// </summary>
    public StripeInvoicesBuilder Invoices() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe invoice item endpoints.
    /// </summary>
    public StripeInvoiceItemsBuilder InvoiceItems() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe product endpoints.
    /// </summary>
    public StripeProductsBuilder Products() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe price endpoints.
    /// </summary>
    public StripePricesBuilder Prices() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe coupon endpoints.
    /// </summary>
    public StripeCouponsBuilder Coupons() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe balance endpoint.
    /// </summary>
    public StripeBalanceBuilder Balance() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe balance transaction endpoints.
    /// </summary>
    public StripeBalanceTransactionsBuilder BalanceTransactions() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe dispute endpoints.
    /// </summary>
    public StripeDisputesBuilder Disputes() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe payout endpoints.
    /// </summary>
    public StripePayoutsBuilder Payouts() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe event endpoints.
    /// </summary>
    public StripeEventsBuilder Events() => new(apiSimulator);

    /// <summary>
    /// Configures Stripe token endpoints.
    /// </summary>
    public StripeTokensBuilder Tokens() => new(apiSimulator);
}
