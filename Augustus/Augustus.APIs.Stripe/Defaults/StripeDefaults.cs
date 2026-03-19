namespace Augustus.APIs.Stripe;

using System.Text.Json;

/// <summary>
/// Provides default realistic Stripe API responses.
/// </summary>
internal static partial class StripeDefaults
{
    private static readonly Random random = new Random();
    private static readonly object randomLock = new object();

    public static string GetDefaultResponse(string resourceType)
    {
        return resourceType switch
        {
            "customer" => GenerateCustomer(),
            "customer_list" => GenerateCustomerList(),
            "customer_deleted" => GenerateDeleted("customer"),
            "charge" => GenerateCharge(),
            "charge_list" => GenerateChargeList(),
            "payment_intent" => GeneratePaymentIntent(),
            "payment_intent_list" => GeneratePaymentIntentList(),
            "payment_intent_canceled" => GeneratePaymentIntent("canceled"),
            "subscription" => GenerateSubscription(),
            "subscription_list" => GenerateSubscriptionList(),
            "subscription_canceled" => GenerateSubscription("canceled"),
            "payment_method" => GeneratePaymentMethod(),
            "payment_method_list" => GeneratePaymentMethodList(),
            "setup_intent" => GenerateSetupIntent(),
            "setup_intent_list" => GenerateSetupIntentList(),
            "setup_intent_canceled" => GenerateSetupIntent("canceled"),
            "refund" => GenerateRefund(),
            "refund_list" => GenerateRefundList(),
            "invoice" => GenerateInvoice(),
            "invoice_list" => GenerateInvoiceList(),
            "invoice_deleted" => GenerateDeleted("invoice"),
            "invoice_finalized" => GenerateInvoice("open"),
            "invoice_paid" => GenerateInvoice("paid"),
            "invoice_voided" => GenerateInvoice("void"),
            "invoice_item" => GenerateInvoiceItem(),
            "invoice_item_list" => GenerateInvoiceItemList(),
            "invoice_item_deleted" => GenerateDeleted("invoiceitem"),
            "product" => GenerateProduct(),
            "product_list" => GenerateProductList(),
            "product_deleted" => GenerateDeleted("product"),
            "price" => GeneratePrice(),
            "price_list" => GeneratePriceList(),
            "coupon" => GenerateCoupon(),
            "coupon_list" => GenerateCouponList(),
            "coupon_deleted" => GenerateDeleted("coupon"),
            "balance" => GenerateBalance(),
            "balance_transaction" => GenerateBalanceTransaction(),
            "balance_transaction_list" => GenerateBalanceTransactionList(),
            "dispute" => GenerateDispute(),
            "dispute_list" => GenerateDisputeList(),
            "dispute_closed" => GenerateDispute("lost"),
            "payout" => GeneratePayout(),
            "payout_list" => GeneratePayoutList(),
            "payout_canceled" => GeneratePayout("canceled"),
            "event" => GenerateEvent(),
            "event_list" => GenerateEventList(),
            "token" => GenerateToken(),
            _ => "{}"
        };
    }

    private static string GenerateList(string url, Func<string> generateItem, int count = 3)
    {
        var items = Enumerable.Range(0, count)
            .Select(_ => JsonSerializer.Deserialize<JsonElement>(generateItem()))
            .ToArray();

        var list = new
        {
            @object = "list",
            data = items,
            has_more = false,
            url = url
        };

        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GenerateDeleted(string objectType)
    {
        var prefix = objectType switch
        {
            "customer" => "cus",
            "invoice" => "in",
            "invoiceitem" => "ii",
            "product" => "prod",
            "coupon" => "coupon",
            _ => objectType.Substring(0, 3)
        };

        var deleted = new
        {
            id = $"{prefix}_{GenerateRandomId()}",
            @object = objectType,
            deleted = true
        };

        return JsonSerializer.Serialize(deleted, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Thread-safe wrapper for Random.Next to prevent race conditions in multi-threaded scenarios.
    /// </summary>
    internal static int GetRandomInt(int minValue, int maxValue)
    {
        lock (randomLock)
        {
            return random.Next(minValue, maxValue);
        }
    }

    internal static string GenerateRandomId(int length = 24)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[GetRandomInt(0, chars.Length)]).ToArray());
    }

    internal static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[GetRandomInt(0, chars.Length)]).ToArray());
    }
}
