namespace Augustus.Sample.StripeApi.Models;

public record CreatePaymentRequest(int Amount, string Currency, string Source);
