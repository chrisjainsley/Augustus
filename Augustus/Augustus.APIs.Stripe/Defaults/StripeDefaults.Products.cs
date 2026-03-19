namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateProduct()
    {
        var product = new
        {
            id = $"prod_{GenerateRandomId()}",
            @object = "product",
            active = true,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            default_price = $"price_{GenerateRandomId()}",
            description = "Test product description",
            images = new string[] { },
            livemode = false,
            metadata = new { },
            name = $"Test Product {GetRandomInt(100, 999)}",
            package_dimensions = (object?)null,
            shippable = (bool?)null,
            statement_descriptor = (string?)null,
            tax_code = (string?)null,
            type = "service",
            unit_label = (string?)null,
            updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            url = (string?)null
        };

        return JsonSerializer.Serialize(product, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GenerateProductList()
    {
        return GenerateList("/v1/products", GenerateProduct, 3);
    }
}
