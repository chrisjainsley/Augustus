namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateDispute(string status = "needs_response")
    {
        var amount = GetRandomInt(1000, 10000);
        var dispute = new
        {
            id = $"dp_{GenerateRandomId()}",
            @object = "dispute",
            amount = amount,
            balance_transactions = new object[] { },
            charge = $"ch_{GenerateRandomId()}",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            evidence = new
            {
                access_activity_log = (string?)null,
                billing_address = (string?)null,
                cancellation_policy = (string?)null,
                cancellation_policy_disclosure = (string?)null,
                cancellation_rebuttal = (string?)null,
                customer_communication = (string?)null,
                customer_email_address = (string?)null,
                customer_name = (string?)null,
                customer_purchase_ip = (string?)null,
                customer_signature = (string?)null,
                duplicate_charge_documentation = (string?)null,
                duplicate_charge_explanation = (string?)null,
                duplicate_charge_id = (string?)null,
                product_description = (string?)null,
                receipt = (string?)null,
                refund_policy = (string?)null,
                refund_policy_disclosure = (string?)null,
                refund_refusal_explanation = (string?)null,
                service_date = (string?)null,
                service_documentation = (string?)null,
                shipping_address = (string?)null,
                shipping_carrier = (string?)null,
                shipping_date = (string?)null,
                shipping_documentation = (string?)null,
                shipping_tracking_number = (string?)null,
                uncategorized_file = (string?)null,
                uncategorized_text = (string?)null
            },
            evidence_details = new
            {
                due_by = DateTimeOffset.UtcNow.AddDays(21).ToUnixTimeSeconds(),
                has_evidence = false,
                past_due = false,
                submission_count = 0
            },
            is_charge_refundable = status == "needs_response",
            livemode = false,
            metadata = new { },
            payment_intent = $"pi_{GenerateRandomId()}",
            reason = "fraudulent",
            status = status
        };

        return JsonSerializer.Serialize(dispute, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GenerateDisputeList()
    {
        return GenerateList("/v1/disputes", () => GenerateDispute(), 2);
    }
}
