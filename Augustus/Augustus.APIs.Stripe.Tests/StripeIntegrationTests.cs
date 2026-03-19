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

    [Fact]
    public async Task StripeMock_NewResources_PaymentMethods()
    {
        var simulator = this.CreateStripeMock();
        simulator.PaymentMethods().Get().UseDefault();
        simulator.PaymentMethods().List().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var pm = await client.GetStringAsync("/v1/payment_methods/pm_123");
            pm.Should().Contain("\"object\":\"payment_method\"");
            pm.Should().Contain("pm_");

            var pmList = await client.GetStringAsync("/v1/payment_methods");
            pmList.Should().Contain("\"object\":\"list\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_NewResources_Refunds()
    {
        var simulator = this.CreateStripeMock();
        simulator.Refunds().Get().UseDefault();
        simulator.Refunds().Create().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var refund = await client.GetStringAsync("/v1/refunds/re_123");
            refund.Should().Contain("\"object\":\"refund\"");
            refund.Should().Contain("re_");

            var createResponse = await client.PostAsync("/v1/refunds", new StringContent("{}"));
            var created = await createResponse.Content.ReadAsStringAsync();
            created.Should().Contain("\"object\":\"refund\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_NewResources_Invoices()
    {
        var simulator = this.CreateStripeMock();
        simulator.Invoices().Get().UseDefault();
        simulator.Invoices().Finalize().UseDefault();
        simulator.Invoices().Pay().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var invoice = await client.GetStringAsync("/v1/invoices/in_123");
            invoice.Should().Contain("\"object\":\"invoice\"");

            var finalizeResponse = await client.PostAsync("/v1/invoices/in_123/finalize", new StringContent("{}"));
            var finalized = await finalizeResponse.Content.ReadAsStringAsync();
            finalized.Should().Contain("\"status\":\"open\"");

            var payResponse = await client.PostAsync("/v1/invoices/in_123/pay", new StringContent("{}"));
            var paid = await payResponse.Content.ReadAsStringAsync();
            paid.Should().Contain("\"status\":\"paid\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_NewResources_Products()
    {
        var simulator = this.CreateStripeMock();
        simulator.Products().Get().UseDefault();
        simulator.Products().List().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var product = await client.GetStringAsync("/v1/products/prod_123");
            product.Should().Contain("\"object\":\"product\"");
            product.Should().Contain("prod_");

            var products = await client.GetStringAsync("/v1/products");
            products.Should().Contain("\"object\":\"list\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_NewResources_Balance()
    {
        var simulator = this.CreateStripeMock();
        simulator.Balance().Get().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var balance = await client.GetStringAsync("/v1/balance");
            balance.Should().Contain("\"object\":\"balance\"");
            balance.Should().Contain("\"available\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_UseAllDefaults_ShouldRegisterAllRoutes()
    {
        var simulator = this.CreateStripeMock();
        simulator.UseAllDefaults();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            // Spot check various resources
            var customer = await client.GetStringAsync("/v1/customers/cus_test");
            customer.Should().Contain("\"object\":\"customer\"");

            var charges = await client.GetStringAsync("/v1/charges");
            charges.Should().Contain("\"object\":\"list\"");

            var pm = await client.GetStringAsync("/v1/payment_methods/pm_test");
            pm.Should().Contain("\"object\":\"payment_method\"");

            var refund = await client.GetStringAsync("/v1/refunds/re_test");
            refund.Should().Contain("\"object\":\"refund\"");

            var balance = await client.GetStringAsync("/v1/balance");
            balance.Should().Contain("\"object\":\"balance\"");

            var events = await client.GetStringAsync("/v1/events");
            events.Should().Contain("\"object\":\"list\"");

            var product = await client.GetStringAsync("/v1/products/prod_test");
            product.Should().Contain("\"object\":\"product\"");

            var price = await client.GetStringAsync("/v1/prices/price_test");
            price.Should().Contain("\"object\":\"price\"");

            var payout = await client.GetStringAsync("/v1/payouts/po_test");
            payout.Should().Contain("\"object\":\"payout\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_WithError_ShouldReturnErrorResponse()
    {
        var simulator = this.CreateStripeMock();
        simulator.Customers().Get("cus_invalid").WithError(404,
            StripeErrors.InvalidRequestError("No such customer: 'cus_invalid'", param: "id"));

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetAsync("/v1/customers/cus_invalid");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("invalid_request_error");
            body.Should().Contain("No such customer");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task StripeMock_UseAllDefaults_IsChainable()
    {
        var simulator = this.CreateStripeMock()
            .UseAllDefaults();

        simulator.Should().NotBeNull();
        simulator.Should().BeOfType<StripeMock>();
    }
}
