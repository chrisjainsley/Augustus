using System.Net;
using Augustus.AI;
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
            options.EnableCaching = true;
        });
        simulator.UseAI(new AIOptions { OpenAIApiKey = "test-key-for-testing" });
        simulator.WithInstruction("Test instruction");

        // Should initialize without throwing
        simulator.Should().NotBeNull();

        // Test clearing cache - should not throw
        var clearingCache = () => simulator.ClearCache();
        clearingCache.Should().NotThrow();
    }

    [Fact]
    public void APISimulator_ShouldHandleMissingApiKey()
    {
        // Test that missing API key is handled gracefully when using AI default handler
        var simulator = this.CreateAPISimulator("Stripe", options =>
        {
            options.EnableCaching = false; // Prevent cache-only auto-detection
            options.Port = 9011;
        });

        var configuringAI = () => simulator.UseAI(new AIOptions
        {
            OpenAIApiKey = "" // Empty API key
        });

        configuringAI.Should()
            .Throw<System.ComponentModel.DataAnnotations.ValidationException>()
            .WithMessage("*OpenAI API key is required*");
    }

    [Fact]
    public void APISimulator_ShouldValidatePortRange()
    {
        var options = new APISimulatorOptions();

        // Test invalid port
        var settingInvalidPort = () => options.Port = 100; // Too low (between 1-1023 is reserved)

        settingInvalidPort.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Port must be 0 (auto-assign) or between 1024 and 65535*");
    }

    [Fact]
    public void FluentAPI_ShouldConfigureRouteSpecificInstructions()
    {
        // Test the new fluent API for route-specific instructions
        var simulator = this.CreateStripeSimulator();
        simulator.UseAI(new AIOptions { OpenAIApiKey = "test-key-for-testing" });
        simulator
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
        var simulator = this.CreateAPISimulator("TestAPI");
        simulator.UseAI(new AIOptions { OpenAIApiKey = "test-key-for-testing" });
        simulator
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
        var simulator = this.CreateAPISimulator("TestAPI");
        simulator.UseAI(new AIOptions { OpenAIApiKey = "test-key-for-testing" });

        // Add global instruction
        simulator.AddInstruction("Global instruction");

        // Add route-specific instruction
        var routeInstruction = new RouteInstruction("/api/test/{id}", "GET");
        routeInstruction.AddInstruction("Route-specific instruction");
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
        // Test default values for core options
        var options = new APISimulatorOptions();

        options.EnableCaching.Should().BeTrue();
        options.CacheFolderPath.Should().Be("./mocks");
        options.Port.Should().Be(9001);
        options.CacheOnly.Should().BeFalse();

        // Test default values for AI options
        var aiOptions = new AIOptions();
        aiOptions.OpenAIModel.Should().Be("gpt-4o-mini");
        aiOptions.OpenAIApiKey.Should().BeEmpty();
        aiOptions.OpenAIEndpoint.Should().BeEmpty();
        aiOptions.MaxRetries.Should().Be(5);
        aiOptions.InitialRetryDelayMs.Should().Be(1000);
        aiOptions.MaxRetryDelayMs.Should().Be(32000);
        aiOptions.MaxConcurrentRequests.Should().Be(10);
    }

    [Fact]
    public void AIOptions_ShouldValidateMaxRetries()
    {
        var aiOptions = new AIOptions();

        // Valid values should not throw
        var settingValidMaxRetries = () => aiOptions.MaxRetries = 5;
        settingValidMaxRetries.Should().NotThrow();

        // Test minimum valid value (0 = no retries)
        aiOptions.MaxRetries = 0;
        aiOptions.MaxRetries.Should().Be(0);

        // Test maximum valid value
        aiOptions.MaxRetries = 10;
        aiOptions.MaxRetries.Should().Be(10);

        // Test invalid values
        var settingNegativeMaxRetries = () => aiOptions.MaxRetries = -1;
        settingNegativeMaxRetries.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxRetries must be between 0 and 10*");

        var settingTooHighMaxRetries = () => aiOptions.MaxRetries = 11;
        settingTooHighMaxRetries.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxRetries must be between 0 and 10*");
    }

    [Fact]
    public void AIOptions_ShouldValidateInitialRetryDelay()
    {
        var aiOptions = new AIOptions();

        // Valid values should not throw
        var settingValidDelay = () => aiOptions.InitialRetryDelayMs = 2000;
        settingValidDelay.Should().NotThrow();
        aiOptions.InitialRetryDelayMs.Should().Be(2000);

        // Test minimum valid value
        aiOptions.InitialRetryDelayMs = 100;
        aiOptions.InitialRetryDelayMs.Should().Be(100);

        // Test maximum valid value
        aiOptions.InitialRetryDelayMs = 60000;
        aiOptions.InitialRetryDelayMs.Should().Be(60000);

        // Test invalid values
        var settingTooLowDelay = () => aiOptions.InitialRetryDelayMs = 99;
        settingTooLowDelay.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*InitialRetryDelayMs must be between 100 and 60000*");

        var settingTooHighDelay = () => aiOptions.InitialRetryDelayMs = 60001;
        settingTooHighDelay.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*InitialRetryDelayMs must be between 100 and 60000*");
    }

    [Fact]
    public void AIOptions_ShouldValidateMaxRetryDelay()
    {
        var aiOptions = new AIOptions();

        // Valid values should not throw
        var settingValidMaxDelay = () => aiOptions.MaxRetryDelayMs = 16000;
        settingValidMaxDelay.Should().NotThrow();
        aiOptions.MaxRetryDelayMs.Should().Be(16000);

        // Test minimum valid value
        aiOptions.MaxRetryDelayMs = 1000;
        aiOptions.MaxRetryDelayMs.Should().Be(1000);

        // Test maximum valid value
        aiOptions.MaxRetryDelayMs = 300000;
        aiOptions.MaxRetryDelayMs.Should().Be(300000);

        // Test invalid values
        var settingTooLowMaxDelay = () => aiOptions.MaxRetryDelayMs = 999;
        settingTooLowMaxDelay.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxRetryDelayMs must be between 1000 and 300000*");

        var settingTooHighMaxDelay = () => aiOptions.MaxRetryDelayMs = 300001;
        settingTooHighMaxDelay.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxRetryDelayMs must be between 1000 and 300000*");
    }

    [Fact]
    public void AIOptions_ShouldValidateMaxConcurrentRequests()
    {
        var aiOptions = new AIOptions();

        // Valid values should not throw
        var settingValidConcurrency = () => aiOptions.MaxConcurrentRequests = 20;
        settingValidConcurrency.Should().NotThrow();
        aiOptions.MaxConcurrentRequests.Should().Be(20);

        // Test minimum valid value
        aiOptions.MaxConcurrentRequests = 1;
        aiOptions.MaxConcurrentRequests.Should().Be(1);

        // Test maximum valid value
        aiOptions.MaxConcurrentRequests = 100;
        aiOptions.MaxConcurrentRequests.Should().Be(100);

        // Test invalid values
        var settingZeroConcurrency = () => aiOptions.MaxConcurrentRequests = 0;
        settingZeroConcurrency.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxConcurrentRequests must be between 1 and 100*");

        var settingTooHighConcurrency = () => aiOptions.MaxConcurrentRequests = 101;
        settingTooHighConcurrency.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxConcurrentRequests must be between 1 and 100*");
    }

    [Fact]
    public void APISimulator_ShouldAllowCustomRetryConfiguration()
    {
        // Test that retry configuration can be customized via AIOptions
        var simulator = this.CreateStripeSimulator();
        simulator.UseAI(new AIOptions
        {
            OpenAIApiKey = "test-key-for-testing",
            MaxRetries = 3,
            InitialRetryDelayMs = 500,
            MaxRetryDelayMs = 8000,
            MaxConcurrentRequests = 5
        });

        simulator.Should().NotBeNull();
    }
}
