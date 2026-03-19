namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateCharge()
    {
        var charge = new
        {
            id = $"ch_{GenerateRandomId()}",
            @object = "charge",
            amount = GetRandomInt(1000, 10000),
            amount_captured = GetRandomInt(1000, 10000),
            amount_refunded = 0,
            application = (string?)null,
            application_fee = (string?)null,
            application_fee_amount = (int?)null,
            balance_transaction = $"txn_{GenerateRandomId()}",
            billing_details = new
            {
                address = new
                {
                    city = (string?)null,
                    country = (string?)null,
                    line1 = (string?)null,
                    line2 = (string?)null,
                    postal_code = (string?)null,
                    state = (string?)null
                },
                email = (string?)null,
                name = (string?)null,
                phone = (string?)null
            },
            calculated_statement_descriptor = "EXAMPLE",
            captured = true,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            customer = $"cus_{GenerateRandomId()}",
            description = "Test charge",
            destination = (string?)null,
            dispute = (string?)null,
            disputed = false,
            failure_balance_transaction = (string?)null,
            failure_code = (string?)null,
            failure_message = (string?)null,
            fraud_details = new { },
            invoice = (string?)null,
            livemode = false,
            metadata = new { },
            on_behalf_of = (string?)null,
            outcome = new
            {
                network_status = "approved_by_network",
                reason = (string?)null,
                risk_level = "normal",
                risk_score = GetRandomInt(10, 50),
                seller_message = "Payment complete.",
                type = "authorized"
            },
            paid = true,
            payment_intent = $"pi_{GenerateRandomId()}",
            payment_method = $"pm_{GenerateRandomId()}",
            payment_method_details = new
            {
                card = new
                {
                    brand = "visa",
                    checks = new
                    {
                        address_line1_check = (string?)null,
                        address_postal_code_check = (string?)null,
                        cvc_check = "pass"
                    },
                    country = "US",
                    exp_month = 12,
                    exp_year = 2025,
                    fingerprint = GenerateRandomId(),
                    funding = "credit",
                    installments = (object?)null,
                    last4 = $"{GetRandomInt(1000, 9999)}",
                    mandate = (string?)null,
                    network = "visa",
                    three_d_secure = (object?)null,
                    wallet = (object?)null
                },
                type = "card"
            },
            receipt_email = (string?)null,
            receipt_number = (string?)null,
            receipt_url = $"https://pay.stripe.com/receipts/{GenerateRandomId()}",
            refunded = false,
            refunds = new
            {
                @object = "list",
                data = new object[] { },
                has_more = false,
                total_count = 0,
                url = "/v1/charges/ch_test/refunds"
            },
            review = (string?)null,
            shipping = (object?)null,
            source = new
            {
                id = $"card_{GenerateRandomId()}",
                @object = "card",
                brand = "Visa",
                country = "US",
                customer = $"cus_{GenerateRandomId()}",
                exp_month = 12,
                exp_year = 2025,
                fingerprint = GenerateRandomId(),
                funding = "credit",
                last4 = $"{GetRandomInt(1000, 9999)}"
            },
            source_transfer = (string?)null,
            statement_descriptor = (string?)null,
            statement_descriptor_suffix = (string?)null,
            status = "succeeded",
            transfer_data = (object?)null,
            transfer_group = (string?)null
        };

        return JsonSerializer.Serialize(charge, SerializerOptions);
    }

    private static string GenerateChargeList()
    {
        return GenerateList("/v1/charges", GenerateCharge, 2);
    }
}
