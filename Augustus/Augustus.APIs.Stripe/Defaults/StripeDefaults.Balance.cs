namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateBalance()
    {
        var balance = new
        {
            @object = "balance",
            available = new[]
            {
                new
                {
                    amount = GetRandomInt(10000, 1000000),
                    currency = "usd",
                    source_types = new
                    {
                        card = GetRandomInt(5000, 500000)
                    }
                }
            },
            connect_reserved = new object[] { },
            livemode = false,
            pending = new[]
            {
                new
                {
                    amount = GetRandomInt(1000, 100000),
                    currency = "usd",
                    source_types = new
                    {
                        card = GetRandomInt(500, 50000)
                    }
                }
            }
        };

        return JsonSerializer.Serialize(balance, SerializerOptions);
    }
}
