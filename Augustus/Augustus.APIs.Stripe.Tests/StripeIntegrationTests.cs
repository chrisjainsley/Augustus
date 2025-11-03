using Augustus;
using Augustus.Extensions;
using Augustus.APIs.Stripe;

namespace Augustus.APIs.Stripe.Tests;

public class StripeIntegrationTests
{
    [Fact]
    public async Task StripeMock_WithDefaultResponse_ShouldReturnRealisticStripeCustomer()
    {
        // Arrange
        var simulator = this.CreateStripeMock();
        simulator.Customers().Get().UseDefault();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Act
            var response = await client.GetStringAsync("/v1/customers/cus_test123");

            // Assert
            response.Should().Contain("\"object\":\"customer\"");
            response.Should().Contain("cus_");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_MultipleEndpoints_ShouldAllWork()
    {
        // Arrange
        var simulator = this.CreateStripeMock();
        simulator.Customers().Get().UseDefault();
        simulator.Customers().List().UseDefault();
        simulator.Charges().Get().UseDefault();
        simulator.PaymentIntents().Create().UseDefault();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Act & Assert
            var customer = await client.GetStringAsync("/v1/customers/cus_123");
            customer.Should().Contain("\"object\":\"customer\"");

            var customers = await client.GetStringAsync("/v1/customers");
            customers.Should().Contain("\"object\":\"list\"");

            var charge = await client.GetStringAsync("/v1/charges/ch_123");
            charge.Should().Contain("\"object\":\"charge\"");

            var piResponse = await client.PostAsync("/v1/payment_intents", new StringContent("{}"));
            var pi = await piResponse.Content.ReadAsStringAsync();
            pi.Should().Contain("\"object\":\"payment_intent\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_DynamicRouteAddition_AfterStart()
    {
        // Arrange
        var simulator = this.CreateStripeMock();
        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Dynamically add Stripe routes after starting
            simulator.Stripe().Subscriptions().Get().UseDefault();

            // Act
            var response = await client.GetStringAsync("/v1/subscriptions/sub_123");

            // Assert
            response.Should().Contain("\"object\":\"subscription\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }
}
