namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateBalanceTransaction()
    {
        var amount = GetRandomInt(1000, 10000);
        var fee = GetRandomInt(30, 500);
        var bt = new
        {
            id = $"txn_{GenerateRandomId()}",
            @object = "balance_transaction",
            amount = amount,
            available_on = DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            description = "Test charge",
            exchange_rate = (double?)null,
            fee = fee,
            fee_details = new[]
            {
                new
                {
                    amount = fee,
                    application = (string?)null,
                    currency = "usd",
                    description = "Stripe processing fees",
                    type = "stripe_fee"
                }
            },
            net = amount - fee,
            reporting_category = "charge",
            source = $"ch_{GenerateRandomId()}",
            status = "available",
            type = "charge"
        };

        return JsonSerializer.Serialize(bt, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GenerateBalanceTransactionList()
    {
        return GenerateList("/v1/balance_transactions", GenerateBalanceTransaction, 3);
    }
}
