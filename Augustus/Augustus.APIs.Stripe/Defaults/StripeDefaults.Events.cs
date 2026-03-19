namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateEvent()
    {
        var evt = new
        {
            id = $"evt_{GenerateRandomId()}",
            @object = "event",
            api_version = "2023-10-16",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            data = new
            {
                @object = new
                {
                    id = $"cus_{GenerateRandomId()}",
                    @object = "customer",
                    email = $"customer{GetRandomInt(1000, 9999)}@example.com"
                }
            },
            livemode = false,
            pending_webhooks = GetRandomInt(0, 5),
            request = new
            {
                id = $"req_{GenerateRandomId()}",
                idempotency_key = (string?)null
            },
            type = "customer.created"
        };

        return JsonSerializer.Serialize(evt, SerializerOptions);
    }

    private static string GenerateEventList()
    {
        return GenerateList("/v1/events", GenerateEvent, 3);
    }
}
