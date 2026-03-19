using System.ComponentModel.DataAnnotations;
using Augustus.AI;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class AzureOpenAITests
{
    [Fact]
    public void CreateAzureOpenAISimulator_ShouldInitializeCorrectly()
    {
        var simulator = this.CreateAzureOpenAISimulator(options =>
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.OpenAIEndpoint = "https://my-resource.openai.azure.com";
            options.AzureDeploymentName = "gpt-4";
        });

        simulator.Should().NotBeNull();
    }

    [Fact]
    public void CreateAzureOpenAISimulator_ShouldAddDefaultInstructions()
    {
        var simulator = this.CreateAzureOpenAISimulator(options =>
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.OpenAIEndpoint = "https://my-resource.openai.azure.com";
            options.AzureDeploymentName = "gpt-4";
        });

        // Verify instructions include Azure-specific content
        var instructions = simulator.InstructionsContainer.Instructions;
        instructions.Should().Contain(i => i.Contains("Azure OpenAI"));
        instructions.Should().Contain(i => i.Contains("content_filter_results"));
        instructions.Should().Contain(i => i.Contains("api-key"));
    }

    [Fact]
    public void CreateAzureOpenAISimulator_ShouldAddRouteInstructions()
    {
        var simulator = this.CreateAzureOpenAISimulator(options =>
        {
            options.OpenAIApiKey = "test-key-for-testing";
            options.OpenAIEndpoint = "https://my-resource.openai.azure.com";
            options.AzureDeploymentName = "gpt-4";
        });

        // Verify route instructions include Azure endpoint patterns
        var routeInstructions = simulator.InstructionsContainer.RouteInstructions;
        routeInstructions.Should().Contain(r => r.Pattern.Contains("/openai/deployments/"));
        routeInstructions.Should().Contain(r => r.Pattern.Contains("chat/completions"));
    }

    [Fact]
    public void RouteInstruction_ShouldMatchAzureDeploymentPatterns()
    {
        var route = new RouteInstruction("/openai/deployments/{deployment}/chat/completions", "POST");

        // Should match valid Azure OpenAI patterns
        route.Matches("/openai/deployments/gpt-4/chat/completions", "POST").Should().BeTrue();
        route.Matches("/openai/deployments/my-deployment/chat/completions", "POST").Should().BeTrue();
        route.Matches("/openai/deployments/gpt-35-turbo/chat/completions", "POST").Should().BeTrue();

        // Should not match standard OpenAI patterns
        route.Matches("/v1/chat/completions", "POST").Should().BeFalse();

        // Should not match wrong HTTP method
        route.Matches("/openai/deployments/gpt-4/chat/completions", "GET").Should().BeFalse();
    }

    [Fact]
    public void RouteInstruction_ShouldMatchAzureEmbeddingsPattern()
    {
        var route = new RouteInstruction("/openai/deployments/{deployment}/embeddings", "POST");

        route.Matches("/openai/deployments/text-embedding-ada-002/embeddings", "POST").Should().BeTrue();
        route.Matches("/openai/deployments/my-embedding/embeddings", "POST").Should().BeTrue();
    }

    [Fact]
    public void AIOptions_ShouldHaveAzureDefaults()
    {
        var options = new AIOptions();

        options.UseAzureOpenAI.Should().BeFalse();
        options.AzureDeploymentName.Should().BeEmpty();
        options.AzureApiVersion.Should().Be("2024-06-01");
    }

    [Fact]
    public void AIOptions_ShouldValidateAzureConfiguration_MissingEndpoint()
    {
        var options = new AIOptions
        {
            OpenAIApiKey = "test-key",
            UseAzureOpenAI = true,
            OpenAIEndpoint = "", // Missing endpoint
            AzureDeploymentName = "gpt-4"
        };

        var validating = () => options.Validate();
        validating.Should().Throw<ValidationException>()
            .WithMessage("*OpenAI endpoint is required when UseAzureOpenAI is true*");
    }

    [Fact]
    public void AIOptions_ShouldValidateAzureConfiguration_MissingDeploymentName()
    {
        var options = new AIOptions
        {
            OpenAIApiKey = "test-key",
            UseAzureOpenAI = true,
            OpenAIEndpoint = "https://my-resource.openai.azure.com",
            AzureDeploymentName = "" // Missing deployment name
        };

        var validating = () => options.Validate();
        validating.Should().Throw<ValidationException>()
            .WithMessage("*Azure deployment name is required when UseAzureOpenAI is true*");
    }

    [Fact]
    public void AIOptions_ShouldValidateAzureConfiguration_ValidConfig()
    {
        var options = new AIOptions
        {
            OpenAIApiKey = "test-key",
            UseAzureOpenAI = true,
            OpenAIEndpoint = "https://my-resource.openai.azure.com",
            AzureDeploymentName = "gpt-4"
        };

        var validating = () => options.Validate();
        validating.Should().NotThrow();
    }

    [Fact]
    public void AIOptions_ShouldNotRequireAzureConfigWhenNotUsingAzure()
    {
        var options = new AIOptions
        {
            OpenAIApiKey = "test-key",
            UseAzureOpenAI = false // Not using Azure
            // OpenAIEndpoint and AzureDeploymentName not set - should be OK
        };

        var validating = () => options.Validate();
        validating.Should().NotThrow();
    }

    [Fact]
    public void AIOptions_AzureApiVersion_ShouldTrimWhitespace()
    {
        var options = new AIOptions();
        options.AzureApiVersion = "  2024-02-01  ";
        options.AzureApiVersion.Should().Be("2024-02-01");
    }

    [Fact]
    public void AIOptions_AzureApiVersion_ShouldUseDefaultWhenEmpty()
    {
        var options = new AIOptions();
        options.AzureApiVersion = "";
        options.AzureApiVersion.Should().Be("2024-06-01");

        options.AzureApiVersion = "   ";
        options.AzureApiVersion.Should().Be("2024-06-01");
    }

    [Fact]
    public void AIOptions_AzureDeploymentName_ShouldTrimWhitespace()
    {
        var options = new AIOptions();
        options.AzureDeploymentName = "  my-deployment  ";
        options.AzureDeploymentName.Should().Be("my-deployment");
    }

    [Fact]
    public void APISimulator_ShouldAcceptAzureConfiguration()
    {
        // Test that simulator can be created with Azure configuration via UseAI
        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.Port = 0;
        });

        simulator.UseAI(new AIOptions
        {
            OpenAIApiKey = "test-key-for-testing",
            UseAzureOpenAI = true,
            OpenAIEndpoint = "https://my-resource.openai.azure.com",
            AzureDeploymentName = "gpt-4"
        });

        simulator.Should().NotBeNull();
    }
}
