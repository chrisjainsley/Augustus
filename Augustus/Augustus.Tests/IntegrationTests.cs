using Augustus;
using Augustus.Extensions;
using Augustus.Stripe;
using FluentAssertions;
using Xunit;

namespace Augustus.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task MockServer_WithStaticResponse_ShouldReturnConfiguredJson()
    {
        // Arrange
        var simulator = this.CreateAPISimulator()
            .ForGet("/api/test")
            .WithResponse(new { message = "Hello World" })
            .Add();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Act
            var response = await client.GetStringAsync("/api/test");

            // Assert
            response.Should().Contain("Hello World");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task MockServer_DynamicRouteAddition_ShouldWorkAfterStart()
    {
        // Arrange
        var simulator = this.CreateAPISimulator();
        await simulator.StartAsync();

        try
        {
            // Add route after starting
            simulator.ForGet("/api/dynamic")
                .WithResponse(new { dynamic = true })
                .Add();

            var client = simulator.CreateClient();

            // Act
            var response = await client.GetStringAsync("/api/dynamic");

            // Assert
            response.Should().Contain("\"dynamic\":true");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

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
    public void MockServer_RemoveRoute_ShouldRemoveConfiguredRoute()
    {
        // Arrange
        var simulator = this.CreateAPISimulator()
            .ForGet("/api/test")
            .WithResponse(new { message = "test" })
            .Add();

        // Act
        var removed = simulator.RemoveRoute("/api/test", "GET");

        // Assert
        removed.Should().BeTrue();
    }

    [Fact]
    public async Task MockServer_ClearRoutes_ShouldRemoveAllRoutes()
    {
        // Arrange
        var simulator = this.CreateAPISimulator()
            .ForGet("/api/test1")
            .WithResponse(new { message = "test1" })
            .Add()
            .ForGet("/api/test2")
            .WithResponse(new { message = "test2" })
            .Add();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Verify routes work before clearing
            var response1 = await client.GetStringAsync("/api/test1");
            response1.Should().Contain("test1");

            // Act - clear all routes
            simulator.ClearRoutes();

            // Assert - verify routes return 404 after clearing
            var response2 = await client.GetAsync("/api/test1");
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        finally
        {
            await simulator.StopAsync();
        }
    }
}
