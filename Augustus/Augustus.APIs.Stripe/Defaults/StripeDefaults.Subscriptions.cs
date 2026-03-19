namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
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

        return JsonSerializer.Serialize(sub, SerializerOptions);
    }

    private static string GenerateSubscriptionList()
    {
        return GenerateList("/v1/subscriptions", () => GenerateSubscription(), 2);
    }
}
