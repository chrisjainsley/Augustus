namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe balance transaction API endpoints.
/// </summary>
public sealed class StripeBalanceTransactionsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeBalanceTransactionsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/balance_transactions/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string balanceTransactionId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/balance_transactions/{balanceTransactionId}", "GET", "balance_transaction");
    }

    /// <summary>
    /// Configures the GET /v1/balance_transactions endpoint (list balance transactions).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/balance_transactions", "GET", "balance_transaction_list");
    }
}
