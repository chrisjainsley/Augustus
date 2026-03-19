namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GeneratePayout(string status = "paid")
    {
        var amount = GetRandomInt(10000, 500000);
        var payout = new
        {
            id = $"po_{GenerateRandomId()}",
            @object = "payout",
            amount = amount,
            arrival_date = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            automatic = true,
            balance_transaction = $"txn_{GenerateRandomId()}",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            description = "STRIPE PAYOUT",
            destination = $"ba_{GenerateRandomId()}",
            failure_balance_transaction = (string?)null,
            failure_code = (string?)null,
            failure_message = (string?)null,
            livemode = false,
            metadata = new { },
            method = "standard",
            original_payout = (string?)null,
            reconciliation_status = status == "paid" ? "completed" : "not_applicable",
            reversed_by = (string?)null,
            source_type = "card",
            statement_descriptor = (string?)null,
            status = status,
            type = "bank_account"
        };

        return JsonSerializer.Serialize(payout, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GeneratePayoutList()
    {
        return GenerateList("/v1/payouts", () => GeneratePayout(), 2);
    }
}
