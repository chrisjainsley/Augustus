using System.Text.Json;
using Augustus.APIs.Stripe;

namespace Augustus.APIs.Stripe.Tests;

public class StripeDefaultsTests
{
    [Theory]
    [InlineData("customer", "customer", "cus_")]
    [InlineData("charge", "charge", "ch_")]
    [InlineData("payment_intent", "payment_intent", "pi_")]
    [InlineData("subscription", "subscription", "sub_")]
    [InlineData("payment_method", "payment_method", "pm_")]
    [InlineData("setup_intent", "setup_intent", "seti_")]
    [InlineData("refund", "refund", "re_")]
    [InlineData("invoice", "invoice", "in_")]
    [InlineData("invoice_item", "invoiceitem", "ii_")]
    [InlineData("product", "product", "prod_")]
    [InlineData("price", "price", "price_")]
    [InlineData("balance_transaction", "balance_transaction", "txn_")]
    [InlineData("dispute", "dispute", "dp_")]
    [InlineData("payout", "payout", "po_")]
    [InlineData("event", "event", "evt_")]
    [InlineData("token", "token", "tok_")]
    public void GetDefaultResponse_SingleResource_ShouldHaveCorrectObjectAndIdPrefix(string resourceType, string expectedObject, string expectedIdPrefix)
    {
        var json = StripeDefaults.GetDefaultResponse(resourceType);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("object").GetString().Should().Be(expectedObject);
        root.GetProperty("id").GetString().Should().StartWith(expectedIdPrefix);
    }

    [Fact]
    public void GetDefaultResponse_Coupon_ShouldHaveCorrectObject()
    {
        var json = StripeDefaults.GetDefaultResponse("coupon");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("object").GetString().Should().Be("coupon");
        // Coupon IDs don't have a prefix, they're just alphanumeric
        root.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetDefaultResponse_Balance_ShouldBeValidSingleton()
    {
        var json = StripeDefaults.GetDefaultResponse("balance");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("object").GetString().Should().Be("balance");
        root.GetProperty("available").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("pending").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("customer_list", "/v1/customers")]
    [InlineData("charge_list", "/v1/charges")]
    [InlineData("payment_intent_list", "/v1/payment_intents")]
    [InlineData("subscription_list", "/v1/subscriptions")]
    [InlineData("payment_method_list", "/v1/payment_methods")]
    [InlineData("setup_intent_list", "/v1/setup_intents")]
    [InlineData("refund_list", "/v1/refunds")]
    [InlineData("invoice_list", "/v1/invoices")]
    [InlineData("invoice_item_list", "/v1/invoiceitems")]
    [InlineData("product_list", "/v1/products")]
    [InlineData("price_list", "/v1/prices")]
    [InlineData("coupon_list", "/v1/coupons")]
    [InlineData("balance_transaction_list", "/v1/balance_transactions")]
    [InlineData("dispute_list", "/v1/disputes")]
    [InlineData("payout_list", "/v1/payouts")]
    [InlineData("event_list", "/v1/events")]
    public void GetDefaultResponse_List_ShouldHaveCorrectStructure(string resourceType, string expectedUrl)
    {
        var json = StripeDefaults.GetDefaultResponse(resourceType);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("object").GetString().Should().Be("list");
        root.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("has_more").GetBoolean().Should().BeFalse();
        root.GetProperty("url").GetString().Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData("customer_deleted", "customer")]
    [InlineData("invoice_deleted", "invoice")]
    [InlineData("product_deleted", "product")]
    [InlineData("coupon_deleted", "coupon")]
    [InlineData("invoice_item_deleted", "invoiceitem")]
    public void GetDefaultResponse_Deleted_ShouldHaveCorrectStructure(string resourceType, string expectedObject)
    {
        var json = StripeDefaults.GetDefaultResponse(resourceType);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("object").GetString().Should().Be(expectedObject);
        root.GetProperty("deleted").GetBoolean().Should().BeTrue();
        root.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetDefaultResponse_PaymentIntentCanceled_ShouldHaveCanceledStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("payment_intent_canceled");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("canceled");
        root.GetProperty("cancellation_reason").GetString().Should().Be("requested_by_customer");
    }

    [Fact]
    public void GetDefaultResponse_SubscriptionCanceled_ShouldHaveCanceledStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("subscription_canceled");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("canceled");
    }

    [Fact]
    public void GetDefaultResponse_SetupIntentCanceled_ShouldHaveCanceledStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("setup_intent_canceled");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("canceled");
        root.GetProperty("cancellation_reason").GetString().Should().Be("abandoned");
    }

    [Fact]
    public void GetDefaultResponse_InvoiceFinalized_ShouldHaveOpenStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("invoice_finalized");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("open");
    }

    [Fact]
    public void GetDefaultResponse_InvoicePaid_ShouldHavePaidStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("invoice_paid");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("paid");
        root.GetProperty("paid").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void GetDefaultResponse_InvoiceVoided_ShouldHaveVoidStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("invoice_voided");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("void");
    }

    [Fact]
    public void GetDefaultResponse_DisputeClosed_ShouldHaveLostStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("dispute_closed");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("lost");
    }

    [Fact]
    public void GetDefaultResponse_PayoutCanceled_ShouldHaveCanceledStatus()
    {
        var json = StripeDefaults.GetDefaultResponse("payout_canceled");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetString().Should().Be("canceled");
    }

    [Fact]
    public void GetDefaultResponse_UnknownResource_ShouldReturnEmptyJson()
    {
        var json = StripeDefaults.GetDefaultResponse("unknown_resource");
        json.Should().Be("{}");
    }

    [Fact]
    public void GetDefaultResponse_Customer_ShouldHaveRequiredFields()
    {
        var json = StripeDefaults.GetDefaultResponse("customer");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("email").GetString().Should().Contain("@example.com");
        root.GetProperty("name").GetString().Should().StartWith("Test Customer");
        root.GetProperty("livemode").GetBoolean().Should().BeFalse();
        root.GetProperty("currency").GetString().Should().Be("usd");
    }

    [Fact]
    public void GetDefaultResponse_PaymentMethod_ShouldHaveCardDetails()
    {
        var json = StripeDefaults.GetDefaultResponse("payment_method");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("card");
        root.GetProperty("card").GetProperty("brand").GetString().Should().Be("visa");
        root.GetProperty("card").GetProperty("last4").GetString().Should().HaveLength(4);
    }

    [Fact]
    public void GetDefaultResponse_Refund_ShouldHaveChargeAndPaymentIntent()
    {
        var json = StripeDefaults.GetDefaultResponse("refund");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("charge").GetString().Should().StartWith("ch_");
        root.GetProperty("payment_intent").GetString().Should().StartWith("pi_");
        root.GetProperty("status").GetString().Should().Be("succeeded");
    }

    [Fact]
    public void GetDefaultResponse_Product_ShouldHaveRequiredFields()
    {
        var json = StripeDefaults.GetDefaultResponse("product");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("active").GetBoolean().Should().BeTrue();
        root.GetProperty("name").GetString().Should().StartWith("Test Product");
        root.GetProperty("type").GetString().Should().Be("service");
    }

    [Fact]
    public void GetDefaultResponse_Price_ShouldHaveRecurring()
    {
        var json = StripeDefaults.GetDefaultResponse("price");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("recurring");
        root.GetProperty("recurring").GetProperty("interval").GetString().Should().Be("month");
        root.GetProperty("unit_amount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultResponse_BalanceTransaction_ShouldHaveFeeDetails()
    {
        var json = StripeDefaults.GetDefaultResponse("balance_transaction");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("fee").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("fee_details").GetArrayLength().Should().BeGreaterThan(0);
        root.GetProperty("net").GetInt32().Should().BeLessThan(root.GetProperty("amount").GetInt32());
    }

    [Fact]
    public void GetDefaultResponse_Event_ShouldHaveTypeAndData()
    {
        var json = StripeDefaults.GetDefaultResponse("event");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("customer.created");
        root.GetProperty("api_version").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("data").GetProperty("object").GetProperty("object").GetString().Should().Be("customer");
    }

    [Fact]
    public void GetDefaultResponse_Token_ShouldHaveCardInfo()
    {
        var json = StripeDefaults.GetDefaultResponse("token");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("type").GetString().Should().Be("card");
        root.GetProperty("used").GetBoolean().Should().BeFalse();
        root.GetProperty("card").GetProperty("brand").GetString().Should().Be("Visa");
    }
}
