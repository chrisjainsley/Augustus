namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateSetupIntent(string status = "succeeded")
    {
        var si = new
        {
            id = $"seti_{GenerateRandomId()}",
            @object = "setup_intent",
            application = (string?)null,
            automatic_payment_methods = (object?)null,
            cancellation_reason = status == "canceled" ? "abandoned" : null,
            client_secret = $"seti_{GenerateRandomId()}_secret_{GenerateRandomId()}",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            customer = $"cus_{GenerateRandomId()}",
            description = (string?)null,
            flow_directions = (object?)null,
            last_setup_error = (object?)null,
            latest_attempt = $"setatt_{GenerateRandomId()}",
            livemode = false,
            mandate = (string?)null,
            metadata = new { },
            next_action = (object?)null,
            on_behalf_of = (string?)null,
            payment_method = $"pm_{GenerateRandomId()}",
            payment_method_configuration_details = (object?)null,
            payment_method_options = new
            {
                card = new
                {
                    mandate_options = (object?)null,
                    network = (string?)null,
                    request_three_d_secure = "automatic"
                }
            },
            payment_method_types = new[] { "card" },
            single_use_mandate = (string?)null,
            status = status,
            usage = "off_session"
        };

        return JsonSerializer.Serialize(si, SerializerOptions);
    }

    private static string GenerateSetupIntentList()
    {
        return GenerateList("/v1/setup_intents", () => GenerateSetupIntent(), 2);
    }
}
