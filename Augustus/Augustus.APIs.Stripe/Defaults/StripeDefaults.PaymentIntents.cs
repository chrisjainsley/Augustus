namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GeneratePaymentIntent(string status = "succeeded")
    {
        var pi = new
        {
            id = $"pi_{GenerateRandomId()}",
            @object = "payment_intent",
            amount = GetRandomInt(1000, 10000),
            amount_capturable = 0,
            amount_details = new
            {
                tip = new { }
            },
            amount_received = status == "succeeded" ? GetRandomInt(1000, 10000) : 0,
            application = (string?)null,
            application_fee_amount = (int?)null,
            automatic_payment_methods = (object?)null,
            canceled_at = status == "canceled" ? (long?)DateTimeOffset.UtcNow.ToUnixTimeSeconds() : null,
            cancellation_reason = status == "canceled" ? "requested_by_customer" : null,
            capture_method = "automatic",
            client_secret = $"pi_{GenerateRandomId()}_secret_{GenerateRandomId()}",
            confirmation_method = "automatic",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            customer = $"cus_{GenerateRandomId()}",
            description = (string?)null,
            invoice = (string?)null,
            last_payment_error = (object?)null,
            latest_charge = status == "succeeded" ? $"ch_{GenerateRandomId()}" : null,
            livemode = false,
            metadata = new { },
            next_action = (object?)null,
            on_behalf_of = (string?)null,
            payment_method = $"pm_{GenerateRandomId()}",
            payment_method_options = new
            {
                card = new
                {
                    installments = (object?)null,
                    mandate_options = (object?)null,
                    network = (string?)null,
                    request_three_d_secure = "automatic"
                }
            },
            payment_method_types = new[] { "card" },
            processing = (object?)null,
            receipt_email = (string?)null,
            review = (string?)null,
            setup_future_usage = (string?)null,
            shipping = (object?)null,
            source = (string?)null,
            statement_descriptor = (string?)null,
            statement_descriptor_suffix = (string?)null,
            status = status,
            transfer_data = (object?)null,
            transfer_group = (string?)null
        };

        return JsonSerializer.Serialize(pi, SerializerOptions);
    }

    private static string GeneratePaymentIntentList()
    {
        return GenerateList("/v1/payment_intents", () => GeneratePaymentIntent(), 2);
    }
}
