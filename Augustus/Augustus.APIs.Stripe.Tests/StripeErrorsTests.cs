using System.Text.Json;
using Augustus.APIs.Stripe;

namespace Augustus.APIs.Stripe.Tests;

public class StripeErrorsTests
{
    [Fact]
    public void InvalidRequestError_ShouldHaveCorrectEnvelope()
    {
        var json = StripeErrors.InvalidRequestError("No such customer: cus_123", param: "customer");
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("invalid_request_error");
        error.GetProperty("message").GetString().Should().Be("No such customer: cus_123");
        error.GetProperty("param").GetString().Should().Be("customer");
    }

    [Fact]
    public void InvalidRequestError_WithoutParam_ShouldOmitParam()
    {
        var json = StripeErrors.InvalidRequestError("Missing required param");
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("invalid_request_error");
        error.GetProperty("message").GetString().Should().Be("Missing required param");
        error.TryGetProperty("param", out _).Should().BeFalse();
    }

    [Fact]
    public void CardError_ShouldHaveCorrectFields()
    {
        var json = StripeErrors.CardError("Your card was declined.", code: "card_declined", declineCode: "insufficient_funds");
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("card_error");
        error.GetProperty("message").GetString().Should().Be("Your card was declined.");
        error.GetProperty("code").GetString().Should().Be("card_declined");
        error.GetProperty("decline_code").GetString().Should().Be("insufficient_funds");
    }

    [Fact]
    public void AuthenticationError_ShouldHaveDefaultMessage()
    {
        var json = StripeErrors.AuthenticationError();
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("authentication_error");
        error.GetProperty("message").GetString().Should().Be("Invalid API Key provided");
    }

    [Fact]
    public void RateLimitError_ShouldHaveDefaultMessage()
    {
        var json = StripeErrors.RateLimitError();
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("rate_limit_error");
        error.GetProperty("message").GetString().Should().Be("Too many requests");
    }

    [Fact]
    public void ApiError_ShouldHaveDefaultMessage()
    {
        var json = StripeErrors.ApiError();
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("api_error");
        error.GetProperty("message").GetString().Should().Be("An internal error occurred");
    }

    [Fact]
    public void IdempotencyError_ShouldHaveCorrectType()
    {
        var json = StripeErrors.IdempotencyError("Keys for idempotent requests can only be used with the same parameters");
        var doc = JsonDocument.Parse(json);
        var error = doc.RootElement.GetProperty("error");

        error.GetProperty("type").GetString().Should().Be("idempotency_error");
        error.GetProperty("message").GetString().Should().Contain("idempotent");
    }

    [Fact]
    public void AllErrors_ShouldProduceValidJson()
    {
        var errors = new[]
        {
            StripeErrors.InvalidRequestError("test"),
            StripeErrors.CardError("test"),
            StripeErrors.AuthenticationError(),
            StripeErrors.RateLimitError(),
            StripeErrors.ApiError(),
            StripeErrors.IdempotencyError("test")
        };

        foreach (var errorJson in errors)
        {
            var parseAction = () => JsonDocument.Parse(errorJson);
            parseAction.Should().NotThrow();

            var doc = JsonDocument.Parse(errorJson);
            doc.RootElement.TryGetProperty("error", out _).Should().BeTrue();
        }
    }
}
