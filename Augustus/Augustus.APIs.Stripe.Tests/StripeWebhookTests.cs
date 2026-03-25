using Augustus;
using Augustus.Extensions;
using Augustus.APIs.Stripe;

namespace Augustus.APIs.Stripe.Tests;

public class StripeWebhookTests
{
    [Fact]
    public async Task PostToSubscriptions_ShouldTriggerWebhookAtReceiver()
    {
        // Arrange - webhook receiver
        await using var receiver = new APISimulator(new APISimulatorOptions { Port = 0 });
        receiver.ForPost("/webhooks/stripe")
            .WithResponse(new { received = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhooks/stripe";

        // Arrange - Stripe simulator with webhook
        var simulator = this.CreateStripeMock(o => o.Port = 0);
        simulator.Subscriptions().Create().UseDefault();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
            .FireWebhookEvent(StripeWebhookEvents.CustomerSubscriptionCreated)
            .WithPayload(new
            {
                id = "evt_test_123",
                type = "customer.subscription.created",
                data = new { @object = new { id = "sub_123", @object = "subscription", status = "active" } }
            })
            .Add();
        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Act
            using var content = new StringContent("{}");
            var response = await client.PostAsync("/v1/subscriptions", content);

            // Wait for webhook delivery
            await simulator.StopAsync();

            // Assert - API response succeeded
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("\"object\":\"subscription\"");

            // Assert - webhook was delivered
            simulator.DeliveredWebhooks.Should().ContainSingle()
                .Which.EventType.Should().Be("customer.subscription.created");
            simulator.DeliveredWebhooks.Should().ContainSingle()
                .Which.Success.Should().BeTrue();
            simulator.DeliveredWebhooks.Should().ContainSingle()
                .Which.Payload.Should().Contain("evt_test_123");
        }
        finally
        {
            await simulator.APISimulator.DisposeAsync();
        }
    }

    [Fact]
    public async Task PostToSubscriptions_WithSigning_ShouldIncludeStripeSignatureHeader()
    {
        // Arrange - webhook receiver
        await using var receiver = new APISimulator(new APISimulatorOptions { Port = 0 });
        receiver.ForPost("/webhooks/stripe")
            .WithResponse(new { received = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhooks/stripe";

        // Arrange - Stripe simulator with webhook signing
        var simulator = this.CreateStripeMock(o => o.Port = 0);
        simulator.Subscriptions().Create().UseDefault();
        simulator.WithWebhook(receiverUrl, signingSecret: "whsec_test_secret");
        simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
            .FireWebhookEvent(StripeWebhookEvents.CustomerSubscriptionCreated)
            .Add();
        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Act
            using var content = new StringContent("{}");
            await client.PostAsync("/v1/subscriptions", content);
            await simulator.StopAsync();

            // Assert - webhook was delivered successfully (signing configured)
            simulator.DeliveredWebhooks.Should().ContainSingle()
                .Which.Success.Should().BeTrue();
        }
        finally
        {
            await simulator.APISimulator.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeleteSubscription_ShouldFireDeletedWebhook()
    {
        await using var receiver = new APISimulator(new APISimulatorOptions { Port = 0 });
        receiver.ForPost("/webhooks/stripe")
            .WithResponse(new { received = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhooks/stripe";

        var simulator = this.CreateStripeMock(o => o.Port = 0);
        simulator.Subscriptions().Cancel().UseDefault();
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Delete, "/v1/subscriptions/{id}")
            .FireWebhookEvent(StripeWebhookEvents.CustomerSubscriptionDeleted)
            .Add();
        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();
            await client.DeleteAsync("/v1/subscriptions/sub_123");
            await simulator.StopAsync();

            simulator.DeliveredWebhooks.Should().ContainSingle()
                .Which.EventType.Should().Be("customer.subscription.deleted");
        }
        finally
        {
            await simulator.APISimulator.DisposeAsync();
        }
    }

    [Fact]
    public void StripeWebhookEvents_ShouldHaveCorrectValues()
    {
        StripeWebhookEvents.CustomerSubscriptionCreated.Should().Be("customer.subscription.created");
        StripeWebhookEvents.PaymentIntentSucceeded.Should().Be("payment_intent.succeeded");
        StripeWebhookEvents.ChargeSucceeded.Should().Be("charge.succeeded");
        StripeWebhookEvents.InvoicePaid.Should().Be("invoice.paid");
        StripeWebhookEvents.CustomerCreated.Should().Be("customer.created");
        StripeWebhookEvents.CheckoutSessionCompleted.Should().Be("checkout.session.completed");
        StripeWebhookEvents.PaymentMethodAttached.Should().Be("payment_method.attached");
    }

    [Fact]
    public void StripeMock_WithWebhook_ShouldBeChainable()
    {
        var simulator = this.CreateStripeMock(o => o.Port = 0);
        var result = simulator.WithWebhook("http://localhost:9999/webhook");
        result.Should().BeSameAs(simulator);
    }
}
