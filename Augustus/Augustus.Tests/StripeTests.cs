using System.Net;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.Tests;

public class StripeTests
{
    [Fact]
    public void APISimulator_ShouldInitializeCorrectly()
    {
        // Test that we can create and configure a simulator without API calls
        var simulator = this.CreateStripeSimulator(options => 
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.EnableCaching = true;
            options.Port = 9010; // Use unique port
        })
        .WithInstruction("Test instruction");

        // Should initialize without throwing
        simulator.Should().NotBeNull();
        
        // Test clearing cache - should not throw
        var clearingCache = () => simulator.ClearCache();
        clearingCache.Should().NotThrow();
    }

    [Fact]
    public void APISimulator_ShouldHandleMissingApiKey()
    {
        // Test that missing API key is handled gracefully
        var options = new APISimulatorOptions
        {
            OpenAIApiKey = "", // Empty API key
            Port = 9011
        };

        var creatingSimulator = () => new APISimulator("Stripe", options);
        
        creatingSimulator.Should()
            .Throw<System.ComponentModel.DataAnnotations.ValidationException>()
            .WithMessage("*OpenAI API key is required*");
    }

    [Fact]
    public void APISimulator_ShouldValidatePortRange()
    {
        var options = new APISimulatorOptions
        {
            OpenAIApiKey = "test-key"
        };

        // Test invalid port
        var settingInvalidPort = () => options.Port = 100; // Too low
        
        settingInvalidPort.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Port must be between 1024 and 65535*");
    }

    [Fact]
    public void FluentAPI_ShouldConfigureRouteSpecificInstructions()
    {
        // Test the new fluent API for route-specific instructions
        var simulator = this.CreateStripeSimulator(options =>
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.Port = 9012;
        })
        .ForGet("/v1/customers/{id}")
            .WithInstruction("Customer exists with provided ID")
            .WithJsonResponse("{\"id\": \"{id}\", \"name\": \"Test Customer\"}")
            .WithStatusCode(200)
        .ForPost("/v1/customers")
            .WithInstruction("Create a new customer")
            .WithStatusCode(201)
        .ForDelete("/v1/customers/{id}")
            .WithInstruction("Customer was successfully deleted")
            .WithStatusCode(204)
        .Build(); // Explicitly call Build() to register route instructions

        simulator.Should().NotBeNull();

        // Verify route instructions were added (using internal accessor for testing)
        var routeInstructions = simulator.InstructionsContainer.RouteInstructions;
        routeInstructions.Should().HaveCount(3);

        // Verify each route instruction has the correct HTTP method and pattern
        routeInstructions.Should().Contain(r => r.HttpMethod == "GET" && r.Pattern == "/v1/customers/{id}");
        routeInstructions.Should().Contain(r => r.HttpMethod == "POST" && r.Pattern == "/v1/customers");
        routeInstructions.Should().Contain(r => r.HttpMethod == "DELETE" && r.Pattern == "/v1/customers/{id}");
    }

    [Fact]
    public void FluentAPI_ShouldSupportMethodSpecificRoutes()
    {
        var simulator = this.CreateAPISimulator("TestAPI", options => 
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.Port = 9013;
        })
        .ConfigureRoutes()
            .ForGet("/api/items/{id}")
                .WithInstruction("Return item details")
                .WithJsonResponse("{\"id\": \"{id}\", \"status\": \"active\"}")
            .ForPut("/api/items/{id}")
                .WithInstruction("Update item with provided data")
                .WithStatusCode(200)
            .ForDelete("/api/items/{id}")
                .WithInstruction("Delete the item")
                .WithStatusCode(204)
        .Build();

        simulator.Should().NotBeNull();
        
        // Verify the ConfigureRoutes and Build pattern works correctly
        var buildResult = () => simulator.ConfigureRoutes().Build();
        buildResult.Should().NotThrow();
    }

    [Fact]
    public void RouteInstruction_ShouldMatchCorrectPatterns()
    {
        // Test route pattern matching
        var routeInstruction = new RouteInstruction("/api/customers/{id}", "GET");
        
        // Should match valid patterns
        routeInstruction.Matches("/api/customers/123", "GET").Should().BeTrue();
        routeInstruction.Matches("/api/customers/cus_abc123", "GET").Should().BeTrue();
        
        // Should not match different HTTP methods
        routeInstruction.Matches("/api/customers/123", "POST").Should().BeFalse();
        
        // Should not match different patterns
        routeInstruction.Matches("/api/orders/123", "GET").Should().BeFalse();
        
        // Should not match incomplete paths
        routeInstruction.Matches("/api/customers", "GET").Should().BeFalse();
    }

    [Fact]
    public void InstructionsContainer_ShouldReturnRouteSpecificInstructions()
    {
        // Create a simulator with a test API
        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.Port = 9014;
        });

        // Add global instruction
        simulator.AddInstruction("Global instruction");

        // Add route-specific instruction
        var routeInstruction = new RouteInstruction("/api/test/{id}", "GET");
        routeInstruction.Instructions.Add("Route-specific instruction");
        simulator.InstructionsContainer.AddRouteInstruction(routeInstruction);

        // Test getting instructions for specific route
        var instructions = simulator.InstructionsContainer.GetInstructionsForRequest("/api/test/123", "GET");

        instructions.Should().Contain("Global instruction");
        instructions.Should().Contain("Route-specific instruction");
        instructions.Should().HaveCountGreaterThan(2); // Should include default instructions too

        // Test getting instructions for different route
        var otherInstructions = simulator.InstructionsContainer.GetInstructionsForRequest("/api/other/123", "GET");
        otherInstructions.Should().Contain("Global instruction");
        otherInstructions.Should().NotContain("Route-specific instruction");
    }

    [Fact]
    public void RouteInstruction_ShouldSupportWildcardMethods()
    {
        // Test route instruction with wildcard method
        var wildcardRoute = new RouteInstruction("/api/health", "*");
        
        // Should match any HTTP method
        wildcardRoute.Matches("/api/health", "GET").Should().BeTrue();
        wildcardRoute.Matches("/api/health", "POST").Should().BeTrue();
        wildcardRoute.Matches("/api/health", "PUT").Should().BeTrue();
        wildcardRoute.Matches("/api/health", "DELETE").Should().BeTrue();
        wildcardRoute.Matches("/api/health", "PATCH").Should().BeTrue();
        
        // Should not match different paths
        wildcardRoute.Matches("/api/status", "GET").Should().BeFalse();
    }

    [Fact]
    public void APISimulatorOptions_ShouldHaveCorrectDefaults()
    {
        // Test default values
        var options = new APISimulatorOptions();
        
        options.EnableCaching.Should().BeTrue();
        options.CacheFolderPath.Should().Be("./mocks");
        options.OpenAIModel.Should().Be("gpt-5-mini");
        options.Port.Should().Be(9001);
        options.OpenAIApiKey.Should().BeEmpty();
        options.OpenAIEndpoint.Should().BeEmpty();
    }
}