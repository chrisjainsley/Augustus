namespace Augustus.APIs.Stripe;

/// <summary>
/// Constants for common Stripe webhook event types.
/// Use with <see cref="WebhookTriggerBuilder.FireWebhookEvent"/> to configure webhook triggers.
/// </summary>
public static class StripeWebhookEvents
{
    // Customers
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string CustomerDeleted = "customer.deleted";

    // Subscriptions
    public const string CustomerSubscriptionCreated = "customer.subscription.created";
    public const string CustomerSubscriptionUpdated = "customer.subscription.updated";
    public const string CustomerSubscriptionDeleted = "customer.subscription.deleted";
    public const string CustomerSubscriptionTrialWillEnd = "customer.subscription.trial_will_end";
    public const string CustomerSubscriptionPaused = "customer.subscription.paused";
    public const string CustomerSubscriptionResumed = "customer.subscription.resumed";

    // Payment Intents
    public const string PaymentIntentCreated = "payment_intent.created";
    public const string PaymentIntentSucceeded = "payment_intent.succeeded";
    public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    public const string PaymentIntentCanceled = "payment_intent.canceled";

    // Invoices
    public const string InvoiceCreated = "invoice.created";
    public const string InvoicePaid = "invoice.paid";
    public const string InvoicePaymentFailed = "invoice.payment_failed";
    public const string InvoiceFinalized = "invoice.finalized";
    public const string InvoiceVoided = "invoice.voided";

    // Charges
    public const string ChargeSucceeded = "charge.succeeded";
    public const string ChargeFailed = "charge.failed";
    public const string ChargeRefunded = "charge.refunded";

    // Checkout Sessions
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string CheckoutSessionExpired = "checkout.session.expired";

    // Payment Methods
    public const string PaymentMethodAttached = "payment_method.attached";
    public const string PaymentMethodDetached = "payment_method.detached";
}
