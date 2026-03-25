using System.Text;
using System.Text.Json;
using Augustus.Extensions;
using static Augustus.Sample.StripeApi.Tests.TestConfiguration;

namespace Augustus.Sample.StripeApi.Tests;

public class WebhookTests
{
    [Fact]
    public async Task CreateSubscription_WithAI_ShouldFireWebhookEvent()
    {
        if (!IsStripeSampleCiCacheOnly() && string.IsNullOrEmpty(GetApiKey()))
            return;

        // Webhook receiver — a tiny simulator that accepts POSTs
        await using var receiver = this.CreateAPISimulator(o => o.Port = 0);
        receiver.ForPost("/webhooks/stripe")
            .WithResponse(new { received = true })
            .Add();
        await receiver.StartAsync();
        var receiverUrl = receiver.CreateClient().BaseAddress + "webhooks/stripe";

        // Stripe simulator with AI instructions + webhook trigger
        await using var simulator = this.CreateStripeSimulator(
                opt => ApplyStripeSimulatorTransport(opt, localPort: 9055))
            .WithInstruction("Return realistic Stripe API JSON responses. Always return raw JSON only, no markdown.")
            .ForPost("/v1/subscriptions")
            .WithInstruction("Return a subscription object with \"object\": \"subscription\", " +
                             "an \"id\" starting with \"sub_\", \"status\": \"active\", " +
                             "and a \"customer\" field starting with \"cus_\".")
            .Build();

        UseStripeAi(simulator);

        // Configure webhook delivery + trigger
        simulator.WithWebhook(receiverUrl);
        simulator.OnRequest(HttpMethod.Post, "/v1/subscriptions")
            .FireWebhookEvent("customer.subscription.created")
            .WithPayload(new
            {
                id = "evt_demo_001",
                type = "customer.subscription.created",
                data = new
                {
                    @object = new
                    {
                        id = "sub_demo_001",
                        @object = "subscription",
                        status = "active",
                        customer = "cus_demo_001"
                    }
                }
            })
            .Add();

        await simulator.StartAsync();
        var client = simulator.CreateClient();

        // Act — create a subscription
        var postBody = """{"customer":"cus_demo_001","items":[{"price":"price_123"}]}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/subscriptions")
        {
            Content = new StringContent(postBody, Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(request);

        // Drain pending webhooks
        await simulator.StopAsync();

        // Assert — AI-generated API response
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("object").GetString().Should().Be("subscription");

        if (IsStripeSampleCiCacheOnly())
            AssertCommittedMockExists("BB7E5933FAEF748B1CD2EA911413DB2B7CFF9A659FF3340430C66C8DBB0EA17A");

        // Assert — webhook was delivered
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.EventType.Should().Be("customer.subscription.created");
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Success.Should().BeTrue();
        simulator.DeliveredWebhooks.Should().ContainSingle()
            .Which.Payload.Should().Contain("evt_demo_001");
    }
}
