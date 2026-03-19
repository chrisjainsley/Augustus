namespace Augustus.APIs.Stripe;

using Augustus;

/// <summary>
/// Builder for configuring Stripe invoice API endpoints.
/// </summary>
public class StripeInvoicesBuilder
{
    private readonly APISimulator apiSimulator;

    internal StripeInvoicesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /v1/invoices/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Get(string invoiceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoices/{invoiceId}", "GET", "invoice");
    }

    /// <summary>
    /// Configures the GET /v1/invoices endpoint (list invoices).
    /// </summary>
    public StripeResourceConfigurer List()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/invoices", "GET", "invoice_list");
    }

    /// <summary>
    /// Configures the POST /v1/invoices endpoint (create invoice).
    /// </summary>
    public StripeResourceConfigurer Create()
    {
        return new StripeResourceConfigurer(apiSimulator, "/v1/invoices", "POST", "invoice");
    }

    /// <summary>
    /// Configures the POST /v1/invoices/:id endpoint (update invoice).
    /// </summary>
    public StripeResourceConfigurer Update(string invoiceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoices/{invoiceId}", "POST", "invoice");
    }

    /// <summary>
    /// Configures the DELETE /v1/invoices/:id endpoint.
    /// </summary>
    public StripeResourceConfigurer Delete(string invoiceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoices/{invoiceId}", "DELETE", "invoice_deleted");
    }

    /// <summary>
    /// Configures the POST /v1/invoices/:id/finalize endpoint.
    /// </summary>
    public StripeResourceConfigurer Finalize(string invoiceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoices/{invoiceId}/finalize", "POST", "invoice_finalized");
    }

    /// <summary>
    /// Configures the POST /v1/invoices/:id/pay endpoint.
    /// </summary>
    public StripeResourceConfigurer Pay(string invoiceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoices/{invoiceId}/pay", "POST", "invoice_paid");
    }

    /// <summary>
    /// Configures the POST /v1/invoices/:id/void endpoint.
    /// </summary>
    public StripeResourceConfigurer Void(string invoiceId = "{id}")
    {
        return new StripeResourceConfigurer(apiSimulator, $"/v1/invoices/{invoiceId}/void", "POST", "invoice_voided");
    }
}
