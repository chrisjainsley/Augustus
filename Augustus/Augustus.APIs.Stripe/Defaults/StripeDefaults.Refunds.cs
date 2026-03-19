namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateRefund()
    {
        var refund = new
        {
            id = $"re_{GenerateRandomId()}",
            @object = "refund",
            amount = GetRandomInt(500, 5000),
            balance_transaction = $"txn_{GenerateRandomId()}",
            charge = $"ch_{GenerateRandomId()}",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            metadata = new { },
            payment_intent = $"pi_{GenerateRandomId()}",
            reason = (string?)null,
            receipt_number = (string?)null,
            source_transfer_reversal = (string?)null,
            status = "succeeded",
            transfer_reversal = (string?)null
        };

        return JsonSerializer.Serialize(refund, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GenerateRefundList()
    {
        return GenerateList("/v1/refunds", GenerateRefund, 2);
    }
}
