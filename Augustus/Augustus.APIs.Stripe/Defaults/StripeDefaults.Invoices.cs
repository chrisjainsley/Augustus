namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateInvoice(string status = "draft")
    {
        var amountDue = GetRandomInt(1000, 10000);
        var invoice = new
        {
            id = $"in_{GenerateRandomId()}",
            @object = "invoice",
            account_country = "US",
            account_name = "Test Business",
            amount_due = amountDue,
            amount_paid = status == "paid" ? amountDue : 0,
            amount_remaining = status == "paid" ? 0 : amountDue,
            amount_shipping = 0,
            application = (string?)null,
            attempt_count = status == "paid" ? 1 : 0,
            attempted = status == "paid",
            auto_advance = status == "draft",
            billing_reason = "manual",
            charge = status == "paid" ? $"ch_{GenerateRandomId()}" : null,
            collection_method = "charge_automatically",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            customer = $"cus_{GenerateRandomId()}",
            customer_email = $"customer{GetRandomInt(1000, 9999)}@example.com",
            customer_name = $"Test Customer {GetRandomInt(100, 999)}",
            default_payment_method = (string?)null,
            description = (string?)null,
            discount = (object?)null,
            due_date = (long?)null,
            effective_at = status != "draft" ? (long?)DateTimeOffset.UtcNow.ToUnixTimeSeconds() : null,
            ending_balance = status == "paid" ? (int?)0 : null,
            footer = (string?)null,
            hosted_invoice_url = status != "draft" ? $"https://invoice.stripe.com/i/{GenerateRandomId()}" : null,
            invoice_pdf = status != "draft" ? $"https://pay.stripe.com/invoice/{GenerateRandomId()}/pdf" : null,
            lines = new
            {
                @object = "list",
                data = new[]
                {
                    new
                    {
                        id = $"il_{GenerateRandomId()}",
                        @object = "line_item",
                        amount = amountDue,
                        currency = "usd",
                        description = "Test line item",
                        livemode = false,
                        metadata = new { },
                        period = new
                        {
                            end = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds(),
                            start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        },
                        quantity = 1,
                        type = "invoiceitem"
                    }
                },
                has_more = false,
                url = $"/v1/invoices/in_test/lines"
            },
            livemode = false,
            metadata = new { },
            next_payment_attempt = (long?)null,
            number = status != "draft" ? $"INV-{GetRandomInt(1000, 9999)}" : null,
            paid = status == "paid",
            paid_out_of_band = false,
            payment_intent = status == "paid" ? $"pi_{GenerateRandomId()}" : null,
            period_end = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            period_start = DateTimeOffset.UtcNow.AddMonths(-1).ToUnixTimeSeconds(),
            post_payment_credit_notes_amount = 0,
            pre_payment_credit_notes_amount = 0,
            receipt_number = (string?)null,
            starting_balance = 0,
            status = status,
            subscription = (string?)null,
            subtotal = amountDue,
            tax = (int?)null,
            total = amountDue,
            webhooks_delivered_at = (long?)null
        };

        return JsonSerializer.Serialize(invoice, SerializerOptions);
    }

    private static string GenerateInvoiceList()
    {
        return GenerateList("/v1/invoices", () => GenerateInvoice(), 2);
    }
}
