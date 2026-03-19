namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateInvoiceItem()
    {
        var amount = GetRandomInt(500, 5000);
        var invoiceItem = new
        {
            id = $"ii_{GenerateRandomId()}",
            @object = "invoiceitem",
            amount = amount,
            currency = "usd",
            customer = $"cus_{GenerateRandomId()}",
            date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            description = "Test invoice item",
            discountable = true,
            discounts = new object[] { },
            invoice = $"in_{GenerateRandomId()}",
            livemode = false,
            metadata = new { },
            period = new
            {
                end = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds(),
                start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            },
            price = new
            {
                id = $"price_{GenerateRandomId()}",
                @object = "price",
                active = true,
                billing_scheme = "per_unit",
                created = DateTimeOffset.UtcNow.AddMonths(-1).ToUnixTimeSeconds(),
                currency = "usd",
                livemode = false,
                metadata = new { },
                product = $"prod_{GenerateRandomId()}",
                type = "one_time",
                unit_amount = amount
            },
            proration = false,
            quantity = 1,
            subscription = (string?)null,
            unit_amount = amount,
            unit_amount_decimal = $"{amount}"
        };

        return JsonSerializer.Serialize(invoiceItem, SerializerOptions);
    }

    private static string GenerateInvoiceItemList()
    {
        return GenerateList("/v1/invoiceitems", GenerateInvoiceItem, 2);
    }
}
