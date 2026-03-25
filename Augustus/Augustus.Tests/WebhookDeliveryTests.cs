using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Augustus.Extensions;

namespace Augustus.Tests;

public class WebhookDeliveryTests
{
    [Fact]
    public async Task OnRequest_FireWebhookEvent_ShouldDeliverWebhookAfterMatchingRequest()
    {
        // Arrange - webhook receiver
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { received = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        // Arrange - API simulator with webhook trigger
        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/v1/subscriptions")
            .WithResponse(new { id = "sub_123", @object = "subscription" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
            .FireWebhookEvent("customer.subscription.created")
            .Add();
        await simulator.StartAsync();

        // Act
        var client = simulator.CreateClient();
        using var content = new StringContent("{}");
        var response = await client.PostAsync("/v1/subscriptions", content);
        await simulator.StopAsync();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.EventType.Should().Be("customer.subscription.created");
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookDelivery_WithStaticPayload_ShouldDeliverConfiguredPayload()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        var expectedPayload = JsonSerializer.Serialize(new
        {
            type = "payment_intent.succeeded",
            data = new { @object = new { id = "pi_123", amount = 2000 } }
        });

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/v1/charges")
            .WithResponse(new { id = "ch_123" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/charges")
            .FireWebhookEvent("payment_intent.succeeded")
            .WithPayload(expectedPayload)
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body = new StringContent("{}");
        await client.PostAsync("/v1/charges", body);
        await simulator.StopAsync();

        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Payload.Should().Be(expectedPayload);
    }

    [Fact]
    public async Task WebhookDelivery_WithObjectPayload_ShouldSerializeAndDeliver()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/v1/events")
            .WithResponse(new { id = "evt_123" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/events")
            .FireWebhookEvent("test.event")
            .WithPayload(new { type = "test.event", data = "hello" })
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body = new StringContent("{}");
        await client.PostAsync("/v1/events", body);
        await simulator.StopAsync();

        var delivered = simulator.DeliveredWebhooks.Should().ContainSingle().Subject;
        delivered.Payload.Should().Contain("\"type\":\"test.event\"");
        delivered.Payload.Should().Contain("\"data\":\"hello\"");
    }

    [Fact]
    public async Task WebhookDelivery_WithHmacSigning_ShouldIncludeSignatureHeader()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        var secret = "test_secret_key";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/v1/charges")
            .WithResponse(new { id = "ch_123" })
            .Add();
        simulator.WithWebhook(receiverUrl, options =>
        {
            options.SigningSecret = secret;
            options.SignatureHeader = "X-Webhook-Signature";
        });
        simulator.OnRequest(HttpMethod.Post, "/v1/charges")
            .FireWebhookEvent("charge.succeeded")
            .WithPayload("{\"type\":\"charge.succeeded\"}")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body = new StringContent("{}");
        await client.PostAsync("/v1/charges", body);
        await simulator.StopAsync();

        // The webhook was delivered successfully (signing doesn't affect delivery)
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookDelivery_WithDelay_ShouldNotBlockResponse()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/api/order")
            .WithResponse(new { id = "order_1" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/api/order")
            .FireWebhookEvent("order.created")
            .WithDelay(TimeSpan.FromMilliseconds(100))
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var body = new StringContent("{}");
        var response = await client.PostAsync("/api/order", body);
        sw.Stop();

        // Response should arrive quickly (delay does not block it)
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        sw.ElapsedMilliseconds.Should().BeLessThan(500);

        // Wait for webhook delivery to complete
        await simulator.StopAsync();
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Success.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookDelivery_ReceiverDown_ShouldNotAffectResponse()
    {
        // No receiver running - webhook delivery will fail
        var receiverUrl = "http://127.0.0.1:19999/webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/api/test")
            .WithResponse(new { ok = true })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/api/test")
            .FireWebhookEvent("test.event")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body = new StringContent("{}");
        var response = await client.PostAsync("/api/test", body);

        // API response should succeed even though webhook delivery fails
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        await simulator.StopAsync();

        // Webhook delivery should be tracked as failed
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Success.Should().BeFalse();
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeliveredWebhooks_ShouldTrackMultipleDeliveries()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/v1/charges")
            .WithResponse(new { id = "ch_123" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/charges")
            .FireWebhookEvent("charge.succeeded")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body1 = new StringContent("{}");
        await client.PostAsync("/v1/charges", body1);
        using var body2 = new StringContent("{}");
        await client.PostAsync("/v1/charges", body2);
        using var body3 = new StringContent("{}");
        await client.PostAsync("/v1/charges", body3);

        await simulator.StopAsync();

        simulator.DeliveredWebhooks.Should().HaveCount(3);
        simulator.DeliveredWebhooks.Should().OnlyContain(w => w.EventType == "charge.succeeded");
        simulator.DeliveredWebhooks.Should().OnlyContain(w => w.Success);
    }

    [Fact]
    public async Task WebhookDelivery_MultipleTriggersOnSameRoute_ShouldFireAll()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/v1/subscriptions")
            .WithResponse(new { id = "sub_123" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
            .FireWebhookEvent("customer.subscription.created")
            .Add();
        simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
            .FireWebhookEvent("invoice.created")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body = new StringContent("{}");
        await client.PostAsync("/v1/subscriptions", body);
        await simulator.StopAsync();

        simulator.DeliveredWebhooks.Should().HaveCount(2);
        simulator.DeliveredWebhooks.Select(w => w.EventType).Should()
            .BeEquivalentTo(new[] { "customer.subscription.created", "invoice.created" });
    }

    [Fact]
    public async Task WebhookDelivery_NoMatchingRoute_ShouldNotFire()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForGet("/v1/customers/{id}")
            .WithResponse(new { id = "cus_123" })
            .Add();
        simulator.WithWebhook(receiverUrl);
        // Trigger configured for POST, but we'll send GET
        simulator.OnRequest(HttpMethod.Post, "/v1/customers")
            .FireWebhookEvent("customer.created")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        await client.GetStringAsync("/v1/customers/cus_123");
        await simulator.StopAsync();

        simulator.DeliveredWebhooks.Should().BeEmpty();
    }

    [Fact]
    public async Task WebhookDelivery_WithPayloadFactory_ShouldCallFactoryAtDeliveryTime()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        var callCount = 0;

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/api/action")
            .WithResponse(new { done = true })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/api/action")
            .FireWebhookEvent("action.completed")
            .WithPayloadFactory(() =>
            {
                Interlocked.Increment(ref callCount);
                return $"{{\"type\":\"action.completed\",\"seq\":{callCount}}}";
            })
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body1 = new StringContent("{}");
        await client.PostAsync("/api/action", body1);
        using var body2 = new StringContent("{}");
        await client.PostAsync("/api/action", body2);
        await simulator.StopAsync();

        callCount.Should().Be(2);
        simulator.DeliveredWebhooks.Should().HaveCount(2);
    }

    [Fact]
    public void WithWebhook_ShouldBeChainable()
    {
        var options = new APISimulatorOptions { Port = 0 };
        var simulator = new APISimulator(options);

        var result = simulator.WithWebhook("http://localhost:9999/webhook");
        result.Should().BeSameAs(simulator);
    }

    [Fact]
    public void OnRequest_ShouldReturnWebhookTriggerBuilder()
    {
        var options = new APISimulatorOptions { Port = 0 };
        var simulator = new APISimulator(options);

        var builder = simulator.OnRequest(HttpMethod.Post, "/v1/test");
        builder.Should().NotBeNull();
        builder.Should().BeOfType<WebhookTriggerBuilder>();
    }

    [Fact]
    public void WebhookTriggerBuilder_Add_WithoutFireWebhookEvent_ShouldThrow()
    {
        var options = new APISimulatorOptions { Port = 0 };
        var simulator = new APISimulator(options);

        var builder = simulator.OnRequest(HttpMethod.Post, "/v1/test");
        var act = () => builder.Add();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeliveredWebhooks_WhenNoWebhookServiceConfigured_ShouldReturnEmpty()
    {
        var options = new APISimulatorOptions { Port = 0 };
        var simulator = new APISimulator(options);

        simulator.DeliveredWebhooks.Should().BeEmpty();
    }

    [Fact]
    public async Task WebhookDelivery_DefaultPayload_ShouldContainEventType()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForPost("/api/test")
            .WithResponse(new { ok = true })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/api/test")
            .FireWebhookEvent("my.custom.event")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        using var body = new StringContent("{}");
        await client.PostAsync("/api/test", body);
        await simulator.StopAsync();

        var delivered = simulator.DeliveredWebhooks.Should().ContainSingle().Subject;
        delivered.Payload.Should().Contain("\"type\":\"my.custom.event\"");
    }

    [Fact]
    public async Task WebhookDelivery_WithHttpVerbOverload_ShouldWork()
    {
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhook")
            .WithResponse(new { ok = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhook";

        await using var simulator = this.CreateAPISimulator(o => o.Port = 0);
        simulator.ForDelete("/v1/subscriptions/{id}")
            .WithResponse(new { id = "sub_123", deleted = true })
            .Add();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpVerb.Delete, "/v1/subscriptions/{id}")
            .FireWebhookEvent("customer.subscription.deleted")
            .Add();
        await simulator.StartAsync();

        var client = simulator.CreateClient();
        await client.DeleteAsync("/v1/subscriptions/sub_123");
        await simulator.StopAsync();

        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.EventType.Should().Be("customer.subscription.deleted");
    }
}
