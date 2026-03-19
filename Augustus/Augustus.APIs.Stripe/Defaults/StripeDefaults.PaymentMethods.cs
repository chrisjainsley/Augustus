namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GeneratePaymentMethod()
    {
        var pm = new
        {
            id = $"pm_{GenerateRandomId()}",
            @object = "payment_method",
            billing_details = new
            {
                address = new
                {
                    city = (string?)null,
                    country = (string?)null,
                    line1 = (string?)null,
                    line2 = (string?)null,
                    postal_code = "10001",
                    state = (string?)null
                },
                email = $"customer{GetRandomInt(1000, 9999)}@example.com",
                name = $"Test Customer {GetRandomInt(100, 999)}",
                phone = (string?)null
            },
            card = new
            {
                brand = "visa",
                checks = new
                {
                    address_line1_check = (string?)null,
                    address_postal_code_check = "pass",
                    cvc_check = "pass"
                },
                country = "US",
                exp_month = 12,
                exp_year = 2027,
                fingerprint = GenerateRandomId(),
                funding = "credit",
                generated_from = (object?)null,
                last4 = $"{GetRandomInt(1000, 9999)}",
                networks = new
                {
                    available = new[] { "visa" },
                    preferred = (string?)null
                },
                three_d_secure_usage = new
                {
                    supported = true
                },
                wallet = (object?)null
            },
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            customer = $"cus_{GenerateRandomId()}",
            livemode = false,
            metadata = new { },
            type = "card"
        };

        return JsonSerializer.Serialize(pm, SerializerOptions);
    }

    private static string GeneratePaymentMethodList()
    {
        return GenerateList("/v1/payment_methods", GeneratePaymentMethod, 3);
    }
}
