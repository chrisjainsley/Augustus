namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe invoice item API endpoints.
/// </summary>
public sealed class StripeInvoiceItemsBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeInvoiceItemsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/invoiceitems/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string invoiceItemId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoiceitems/{invoiceItemId}", "GET", "invoice_item");
    }

    /// <summary>
    /// Configures the GET /v1/invoiceitems endpoint (list invoice items).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/invoiceitems", "GET", "invoice_item_list");
    }

    /// <summary>
    /// Configures the POST /v1/invoiceitems endpoint (create invoice item).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/invoiceitems", "POST", "invoice_item");
    }

    /// <summary>
    /// Configures the POST /v1/invoiceitems/:id endpoint (update invoice item).
    /// </summary>
    public StripeResourceConfigurer Update(string invoiceItemId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoiceitems/{invoiceItemId}", "POST", "invoice_item");
    }

    /// <summary>
    /// Configures the DELETE /v1/invoiceitems/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Delete(string invoiceItemId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoiceitems/{invoiceItemId}", "DELETE", "invoice_item_deleted");
    }
}
