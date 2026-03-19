namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateCustomer()
    {
        var customer = new
        {
            id = $"cus_{GenerateRandomId()}",
            @object = "customer",
            address = (object?)null,
            balance = 0,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            default_source = (string?)null,
            delinquent = false,
            description = "Test customer",
            discount = (object?)null,
            email = $"customer{GetRandomInt(1000, 9999)}@example.com",
            invoice_prefix = GenerateRandomString(8).ToUpper(),
            invoice_settings = new
            {
                custom_fields = (object?)null,
                default_payment_method = (string?)null,
                footer = (string?)null,
                rendering_options = (object?)null
            },
            livemode = false,
            metadata = new { },
            name = $"Test Customer {GetRandomInt(100, 999)}",
            phone = (string?)null,
            preferred_locales = new string[] { },
            shipping = (object?)null,
            tax_exempt = "none",
            test_clock = (string?)null
        };

        return JsonSerializer.Serialize(customer, SerializerOptions);
    }

    private static string GenerateCustomerList()
    {
        return GenerateList("/v1/customers", GenerateCustomer, 3);
    }
}
