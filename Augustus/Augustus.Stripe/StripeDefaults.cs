namespace Augustus.Stripe;

using System.Text.Json;

/// <summary>
/// Provides default realistic Stripe API responses.
/// </summary>
internal static class StripeDefaults
{
    private static readonly Random random = new Random();

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
            _ => "{}"
        };
    }

    private static string GenerateCustomer()
    {
        var customer = new
        {
            id = $"cus_{GenerateRandomId()}",
            @object = "customer",
            address = (object?)null,
            balance = 0,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = "usd",
            default_source = (string?)null,
            delinquent = false,
            description = "Test customer",
            discount = (object?)null,
            email = $"customer{random.Next(1000, 9999)}@example.com",
            invoice_prefix = GenerateRandomString(8).ToUpper(),
            invoice_settings = new
            {
                custom_fields = (object?)null,
                default_payment_method = (string?)null,
                footer = (string?)null,
                rendering_options = (object?)null
            },
            livemode = false,
            metadata = new { },
            name = $"Test Customer {random.Next(100, 999)}",
            phone = (string?)null,
            preferred_locales = new string[] { },
            shipping = (object?)null,
            tax_exempt = "none",
            test_clock = (string?)null
        };

        return JsonSerializer.Serialize(customer, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateCustomerList()
    {
        var customers = new[]
        {
            JsonSerializer.Deserialize<JsonElement>(GenerateCustomer()),
            JsonSerializer.Deserialize<JsonElement>(GenerateCustomer()),
            JsonSerializer.Deserialize<JsonElement>(GenerateCustomer())
        };

        var list = new
        {
            @object = "list",
            data = customers,
            has_more = false,
            url = "/v1/customers"
        };

        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateCharge()
    {
        var charge = new
        {
            id = $"ch_{GenerateRandomId()}",
            @object = "charge",
            amount = random.Next(1000, 10000),
            amount_captured = random.Next(1000, 10000),
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
                risk_score = random.Next(10, 50),
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
                    last4 = $"{random.Next(1000, 9999)}",
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
                last4 = $"{random.Next(1000, 9999)}"
            },
            source_transfer = (string?)null,
            statement_descriptor = (string?)null,
            statement_descriptor_suffix = (string?)null,
            status = "succeeded",
            transfer_data = (object?)null,
            transfer_group = (string?)null
        };

        return JsonSerializer.Serialize(charge, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateChargeList()
    {
        var charges = new[]
        {
            JsonSerializer.Deserialize<JsonElement>(GenerateCharge()),
            JsonSerializer.Deserialize<JsonElement>(GenerateCharge())
        };

        var list = new
        {
            @object = "list",
            data = charges,
            has_more = false,
            url = "/v1/charges"
        };

        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GeneratePaymentIntent(string status = "succeeded")
    {
        var pi = new
        {
            id = $"pi_{GenerateRandomId()}",
            @object = "payment_intent",
            amount = random.Next(1000, 10000),
            amount_capturable = 0,
            amount_details = new
            {
                tip = new { }
            },
            amount_received = status == "succeeded" ? random.Next(1000, 10000) : 0,
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

        return JsonSerializer.Serialize(pi, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GeneratePaymentIntentList()
    {
        var paymentIntents = new[]
        {
            JsonSerializer.Deserialize<JsonElement>(GeneratePaymentIntent()),
            JsonSerializer.Deserialize<JsonElement>(GeneratePaymentIntent())
        };

        var list = new
        {
            @object = "list",
            data = paymentIntents,
            has_more = false,
            url = "/v1/payment_intents"
        };

        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateSubscription(string status = "active")
    {
        var sub = new
        {
            id = $"sub_{GenerateRandomId()}",
            @object = "subscription",
            application = (string?)null,
            application_fee_percent = (double?)null,
            automatic_tax = new
            {
                enabled = false
            },
            billing_cycle_anchor = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            billing_thresholds = (object?)null,
            cancel_at = (long?)null,
            cancel_at_period_end = false,
            canceled_at = status == "canceled" ? (long?)DateTimeOffset.UtcNow.ToUnixTimeSeconds() : null,
            cancellation_details = new
            {
                comment = (string?)null,
                feedback = (string?)null,
                reason = status == "canceled" ? "cancellation_requested" : null
            },
            collection_method = "charge_automatically",
            created = DateTimeOffset.UtcNow.AddMonths(-1).ToUnixTimeSeconds(),
            currency = "usd",
            current_period_end = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds(),
            current_period_start = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            customer = $"cus_{GenerateRandomId()}",
            days_until_due = (int?)null,
            default_payment_method = $"pm_{GenerateRandomId()}",
            default_source = (string?)null,
            default_tax_rates = new object[] { },
            description = (string?)null,
            discount = (object?)null,
            ended_at = status == "canceled" ? (long?)DateTimeOffset.UtcNow.ToUnixTimeSeconds() : null,
            items = new
            {
                @object = "list",
                data = new[]
                {
                    new
                    {
                        id = $"si_{GenerateRandomId()}",
                        @object = "subscription_item",
                        billing_thresholds = (object?)null,
                        created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        metadata = new { },
                        plan = new
                        {
                            id = $"price_{GenerateRandomId()}",
                            @object = "plan",
                            active = true,
                            amount = 999,
                            currency = "usd",
                            interval = "month",
                            interval_count = 1,
                            nickname = (string?)null,
                            product = $"prod_{GenerateRandomId()}",
                            usage_type = "licensed"
                        },
                        price = new
                        {
                            id = $"price_{GenerateRandomId()}",
                            @object = "price",
                            active = true,
                            billing_scheme = "per_unit",
                            created = DateTimeOffset.UtcNow.AddMonths(-2).ToUnixTimeSeconds(),
                            currency = "usd",
                            custom_unit_amount = (object?)null,
                            livemode = false,
                            lookup_key = (string?)null,
                            metadata = new { },
                            nickname = (string?)null,
                            product = $"prod_{GenerateRandomId()}",
                            recurring = new
                            {
                                aggregate_usage = (string?)null,
                                interval = "month",
                                interval_count = 1,
                                usage_type = "licensed"
                            },
                            tax_behavior = "unspecified",
                            tiers_mode = (string?)null,
                            transform_quantity = (object?)null,
                            type = "recurring",
                            unit_amount = 999,
                            unit_amount_decimal = "999"
                        },
                        quantity = 1,
                        subscription = $"sub_{GenerateRandomId()}",
                        tax_rates = new object[] { }
                    }
                },
                has_more = false,
                total_count = 1,
                url = $"/v1/subscription_items?subscription=sub_test"
            },
            latest_invoice = $"in_{GenerateRandomId()}",
            livemode = false,
            metadata = new { },
            next_pending_invoice_item_invoice = (long?)null,
            on_behalf_of = (string?)null,
            pause_collection = (object?)null,
            payment_settings = new
            {
                payment_method_options = (object?)null,
                payment_method_types = (object?)null,
                save_default_payment_method = "off"
            },
            pending_invoice_item_interval = (object?)null,
            pending_setup_intent = (string?)null,
            pending_update = (object?)null,
            schedule = (string?)null,
            start_date = DateTimeOffset.UtcNow.AddMonths(-1).ToUnixTimeSeconds(),
            status = status,
            test_clock = (string?)null,
            transfer_data = (object?)null,
            trial_end = (long?)null,
            trial_settings = new
            {
                end_behavior = new
                {
                    missing_payment_method = "create_invoice"
                }
            },
            trial_start = (long?)null
        };

        return JsonSerializer.Serialize(sub, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateSubscriptionList()
    {
        var subscriptions = new[]
        {
            JsonSerializer.Deserialize<JsonElement>(GenerateSubscription()),
            JsonSerializer.Deserialize<JsonElement>(GenerateSubscription())
        };

        var list = new
        {
            @object = "list",
            data = subscriptions,
            has_more = false,
            url = "/v1/subscriptions"
        };

        return JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateDeleted(string objectType)
    {
        var deleted = new
        {
            id = $"{objectType.Substring(0, 3)}_{GenerateRandomId()}",
            @object = objectType,
            deleted = true
        };

        return JsonSerializer.Serialize(deleted, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string GenerateRandomId(int length = 24)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
