namespace Augustus.APIs.Stripe;

using System.Text.Json;

internal static partial class StripeDefaults
{
    private static string GenerateCoupon()
    {
        var coupon = new
        {
            id = GenerateRandomId(8).ToUpper(),
            @object = "coupon",
            amount_off = (int?)null,
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            currency = (string?)null,
            duration = "repeating",
            duration_in_months = 3,
            livemode = false,
            max_redemptions = (int?)null,
            metadata = new { },
            name = $"Test Coupon {GetRandomInt(100, 999)}",
            percent_off = (double?)GetRandomInt(10, 50),
            redeem_by = (long?)null,
            times_redeemed = GetRandomInt(0, 100),
            valid = true
        };

        return JsonSerializer.Serialize(coupon, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GenerateCouponList()
    {
        return GenerateList("/v1/coupons", GenerateCoupon, 2);
    }
}
