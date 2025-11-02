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
        var mock = this.CreateMockServer()
            .ForGet("/api/test")
            .WithResponse(new { message = "Hello World" })
            .Add();

        await mock.StartAsync();

        try
        {
            var client = mock.CreateClient();

            // Act
            var response = await client.GetStringAsync("/api/test");

            // Assert
            response.Should().Contain("Hello World");
        }
        finally
        {
            await mock.StopAsync();
        }
    }

    [Fact]
    public async Task MockServer_DynamicRouteAddition_ShouldWorkAfterStart()
    {
        // Arrange
        var mock = this.CreateMockServer();
        await mock.StartAsync();

        try
        {
            // Add route after starting
            mock.ForGet("/api/dynamic")
                .WithResponse(new { dynamic = true })
                .Add();

            var client = mock.CreateClient();

            // Act
            var response = await client.GetStringAsync("/api/dynamic");

            // Assert
            response.Should().Contain("\"dynamic\":true");
        }
        finally
        {
            await mock.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_WithDefaultResponse_ShouldReturnRealisticStripeCustomer()
    {
        // Arrange
        var mock = this.CreateStripeMock();
        mock.Customers().Get().UseDefault();

        await mock.StartAsync();

        try
        {
            var client = mock.CreateClient();

            // Act
            var response = await client.GetStringAsync("/v1/customers/cus_test123");

            // Assert
            response.Should().Contain("\"object\":\"customer\"");
            response.Should().Contain("cus_");
        }
        finally
        {
            await mock.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_MultipleEndpoints_ShouldAllWork()
    {
        // Arrange
        var mock = this.CreateStripeMock();
        mock.Customers().Get().UseDefault();
        mock.Customers().List().UseDefault();
        mock.Charges().Get().UseDefault();
        mock.PaymentIntents().Create().UseDefault();

        await mock.StartAsync();

        try
        {
            var client = mock.CreateClient();

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
            await mock.StopAsync();
        }
    }

    [Fact]
    public void MockServer_RemoveRoute_ShouldRemoveConfiguredRoute()
    {
        // Arrange
        var mock = this.CreateMockServer()
            .ForGet("/api/test")
            .WithResponse(new { message = "test" })
            .Add();

        // Act
        var removed = mock.RemoveRoute("/api/test", "GET");

        // Assert
        removed.Should().BeTrue();
    }

    [Fact]
    public async Task MockServer_ClearRoutes_ShouldRemoveAllRoutes()
    {
        // Arrange
        var mock = this.CreateMockServer()
            .ForGet("/api/test1")
            .WithResponse(new { message = "test1" })
            .Add()
            .ForGet("/api/test2")
            .WithResponse(new { message = "test2" })
            .Add();

        await mock.StartAsync();

        try
        {
            var client = mock.CreateClient();

            // Verify routes work before clearing
            var response1 = await client.GetStringAsync("/api/test1");
            response1.Should().Contain("test1");

            // Act - clear all routes
            mock.ClearRoutes();

            // Assert - verify routes return 404 after clearing
            var response2 = await client.GetAsync("/api/test1");
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        finally
        {
            await mock.StopAsync();
        }
    }
}
