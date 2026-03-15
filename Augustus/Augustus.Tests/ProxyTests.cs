using System.ComponentModel.DataAnnotations;
using System.Text;
using Augustus;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.Tests;

public class ProxyTests
{
    [Fact]
    public void ProxyMode_RequiresUpstreamEndpoint()
    {
        var options = new APISimulatorOptions
        {
            OpenAIApiKey = "test-key",
            ProxyMode = true
        };

        var act = () => options.Validate();

        act.Should().Throw<ValidationException>()
            .WithMessage("*ProxyUpstreamEndpoint*");
    }

    [Fact]
    public void ProxyMode_RequiresApiKey()
    {
        var options = new APISimulatorOptions
        {
            ProxyMode = true,
            ProxyUpstreamEndpoint = "https://api.openai.com"
        };

        var act = () => options.Validate();

        act.Should().Throw<ValidationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public void ProxyMode_RequiresValidUri()
    {
        var options = new APISimulatorOptions
        {
            OpenAIApiKey = "test-key",
            ProxyMode = true,
            ProxyUpstreamEndpoint = "not-a-uri"
        };

        var act = () => options.Validate();

        act.Should().Throw<ValidationException>()
            .WithMessage("*valid absolute URI*");
    }

    [Fact]
    public void ProxyMode_ValidatesSuccessfully()
    {
        var options = new APISimulatorOptions
        {
            OpenAIApiKey = "test-key",
            ProxyMode = true,
            ProxyUpstreamEndpoint = "https://api.openai.com"
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void ComputeCacheKey_IsDeterministic()
    {
        var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\",\"messages\":[]}");

        var key1 = ProxyResponseGenerator.ComputeCacheKey("POST", "/v1/chat/completions", "?api-version=2024-06-01", body);
        var key2 = ProxyResponseGenerator.ComputeCacheKey("POST", "/v1/chat/completions", "?api-version=2024-06-01", body);

        key1.Should().Be(key2);
    }

    [Fact]
    public void ComputeCacheKey_DifferentInputsProduceDifferentKeys()
    {
        var body1 = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
        var body2 = Encoding.UTF8.GetBytes("{\"model\":\"gpt-3.5-turbo\"}");

        var key1 = ProxyResponseGenerator.ComputeCacheKey("POST", "/v1/chat/completions", null, body1);
        var key2 = ProxyResponseGenerator.ComputeCacheKey("POST", "/v1/chat/completions", null, body2);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void CreateOpenAIProxy_SetsProxyMode()
    {
        var simulator = this.CreateOpenAIProxy(opt =>
        {
            opt.OpenAIApiKey = "test-key";
            opt.ProxyUpstreamEndpoint = "https://api.openai.com";
        });

        simulator.Should().NotBeNull();
    }

    [Fact]
    public void CreateAzureOpenAIProxy_SetsProxyMode()
    {
        var simulator = this.CreateAzureOpenAIProxy(opt =>
        {
            opt.OpenAIApiKey = "test-key";
            opt.ProxyUpstreamEndpoint = "https://my-resource.openai.azure.com";
        });

        simulator.Should().NotBeNull();
    }

    [Fact]
    public void ProxyTimeoutSeconds_RejectsInvalidValues()
    {
        var options = new APISimulatorOptions();

        var act = () => options.ProxyTimeoutSeconds = 0;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProxyTimeoutSeconds_DefaultIs120()
    {
        var options = new APISimulatorOptions();

        options.ProxyTimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void ProxyMode_RejectsCacheOnlyCombination()
    {
        var options = new APISimulatorOptions
        {
            OpenAIApiKey = "test-key",
            ProxyMode = true,
            CacheOnly = true,
            ProxyUpstreamEndpoint = "https://api.openai.com"
        };

        var act = () => options.Validate();

        act.Should().Throw<ValidationException>()
            .WithMessage("*ProxyMode*CacheOnly*");
    }
}
