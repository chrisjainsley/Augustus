namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GeneratePrice()
    {
        var unitAmount = GetRandomInt(500, 10000);
        var price = new
        {
            id = $"price_{GenerateRandomId()}",
            @object = "price",
            active = true,
            billing_scheme = "per_unit",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            custom_unit_amount = (object?)null,
            livemode = false,
            lookup_key = (string?)null,
            metadata = new { },
            nickname = (string?)null,
            product = $"prod_{GenerateRandomId()}",
            recurring = new
            {
                aggregate_usage = (string?)null,
                interval = "month",
                interval_count = 1,
                usage_type = "licensed"
            },
            tax_behavior = "unspecified",
            tiers_mode = (string?)null,
            transform_quantity = (object?)null,
            type = "recurring",
            unit_amount = unitAmount,
            unit_amount_decimal = $"{unitAmount}"
        };

        return JsonSerializer.Serialize(price, SerializerOptions);
    }

    private static string GeneratePriceList()
    {
        return GenerateList("/v1/prices", GeneratePrice, 3);
    }
}
