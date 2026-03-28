using System.Security.Cryptography;
using System.Text;
using Augustus;
using Augustus.Extensions;
using Augustus.APIs.Stripe;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    public async Task PostToSubscriptions_WithSigning_ShouldIncludeValidStripeSignatureHeader()
    {
        // Arrange - receiver that captures the Stripe-Signature header and body
        string? capturedSignatureHeader = null;
        string? capturedBody = null;
        var receiverHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseUrls("http://127.0.0.1:0");
                web.Configure(app => app.Run(async ctx =>
                {
                    capturedSignatureHeader = ctx.Request.Headers["Stripe-Signature"];
                    using var reader = new StreamReader(ctx.Request.Body);
                    capturedBody = await reader.ReadToEndAsync();
                    ctx.Response.StatusCode = 200;
                }));
            })
            .Build();
        await receiverHost.StartAsync();
        var server = receiverHost.Services.GetRequiredService<IServer>();
        var receiverUrl = server.Features.Get<IServerAddressesFeature>()!.Addresses.First() + "/webhooks/stripe";

        var signingSecret = "whsec_test_secret";

        // Arrange - Stripe simulator with webhook signing
        var simulator = this.CreateStripeMock(o => o.Port = 0);
        simulator.Subscriptions().Create().UseDefault();
        simulator.WithWebhook(receiverUrl, signingSecret: signingSecret);
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
            await receiverHost.StopAsync();

            // Assert - Stripe-Signature header was present with t=...,v1=... format
            capturedSignatureHeader.Should().NotBeNullOrEmpty();
            capturedBody.Should().NotBeNullOrEmpty();

            // Parse the Stripe-Signature header: "t={timestamp},v1={hmac}"
            var parts = capturedSignatureHeader!.Split(',');
            parts.Should().HaveCount(2);
            parts[0].Should().StartWith("t=");
            parts[1].Should().StartWith("v1=");

            var timestamp = parts[0]["t=".Length..];
            var receivedHmac = parts[1]["v1=".Length..];

            // Verify the HMAC: Stripe signs "{timestamp}.{payload}"
            var signedPayload = $"{timestamp}.{capturedBody}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
            var expectedHmac = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
            receivedHmac.Should().Be(expectedHmac);

            // Assert - delivery was tracked as successful
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
