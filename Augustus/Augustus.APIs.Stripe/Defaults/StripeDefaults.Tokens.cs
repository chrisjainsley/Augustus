namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateToken()
    {
        var token = new
        {
            id = $"tok_{GenerateRandomId()}",
            @object = "token",
            card = new
            {
                id = $"card_{GenerateRandomId()}",
                @object = "card",
                address_city = (string?)null,
                address_country = (string?)null,
                address_line1 = (string?)null,
                address_line2 = (string?)null,
                address_state = (string?)null,
                address_zip = (string?)null,
                address_zip_check = (string?)null,
                brand = "Visa",
                country = "US",
                cvc_check = "unchecked",
                exp_month = 12,
                exp_year = 2027,
                fingerprint = GenerateRandomId(),
                funding = "credit",
                last4 = $"{GetRandomInt(1000, 9999)}",
                name = (string?)null,
                tokenization_method = (string?)null
            },
            client_ip = "127.0.0.1",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            livemode = false,
            type = "card",
            used = false
        };

        return JsonSerializer.Serialize(token, SerializerOptions);
    }
}
