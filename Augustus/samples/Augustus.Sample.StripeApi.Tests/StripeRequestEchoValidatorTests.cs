using System.Text.Json;
using Augustus.AI;

namespace Augustus.Sample.StripeApi.Tests;

public class StripeRequestEchoValidatorTests
{
    private readonly StripeRequestEchoValidator validator = new();

    [Theory]
    [InlineData("{\"object\":\"charge\",\"amount\":2000,\"currency\":\"USD\"}", true)]
    [InlineData("{\"object\":\"charge\",\"amount\":200,\"currency\":\"usd\"}", false)]
    [InlineData("{\"object\":\"charge\",\"amount\":2000,\"currency\":\"eur\"}", false)]
    public async Task FormBody_AmountAndCurrencyMustEcho(string response, bool valid)
    {
        var context = Context("POST", "/v1/charges", " -d 'amount=2000&currency=usd&source=tok_visa'");

        var result = await validator.ValidateAsync(context, Json(response), CancellationToken.None);

        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public async Task Mismatch_NamesTheField()
    {
        var context = Context("POST", "/v1/charges", " -d 'amount=2000&currency=usd'");

        var result = await validator.ValidateAsync(context, Json("{\"amount\":200}"), CancellationToken.None);

        result.Reason.Should().Contain("\"amount\"").And.Contain("2000");
    }

    [Fact]
    public async Task JsonBody_CustomerMustEcho()
    {
        var context = Context("POST", "/v1/subscriptions", " -d '{\"customer\":\"cus_demo_001\",\"items\":[{\"price\":\"price_123\"}]}'");

        (await validator.ValidateAsync(context, Json("{\"customer\":\"cus_demo_001\"}"), CancellationToken.None)).IsValid.Should().BeTrue();
        (await validator.ValidateAsync(context, Json("{\"customer\":\"cus_other\"}"), CancellationToken.None)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ExpandedObject_IsNotAMismatch()
    {
        var context = Context("POST", "/v1/charges", " -d 'customer=cus_1'");

        var result = await validator.ValidateAsync(context, Json("{\"customer\":{\"id\":\"cus_1\"}}"), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Retrieve_IdMustMatchPath()
    {
        var context = Context("GET", "/v1/customers/cus_test123", string.Empty);

        (await validator.ValidateAsync(context, Json("{\"id\":\"cus_test123\"}"), CancellationToken.None)).IsValid.Should().BeTrue();
        (await validator.ValidateAsync(context, Json("{\"id\":\"cus_other\"}"), CancellationToken.None)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task List_IsNotCheckedForId()
    {
        var context = Context("GET", "/v1/customers", string.Empty);

        var result = await validator.ValidateAsync(context, Json("{\"object\":\"list\",\"data\":[]}"), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    private static AIGenerationContext Context(string method, string path, string data)
        => new(method, path, $"curl -X {method}{data} \"http://localhost{path}\"", new[] { "Return Stripe JSON." });

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;
}
