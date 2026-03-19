using System.ComponentModel.DataAnnotations;
using Augustus.AI;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class ProxyTests
{
    [Fact]
    public void ProxyMode_RequiresUpstreamEndpoint()
    {
        var act = () => this.CreateOpenAIProxy(opt =>
        {
            opt.OpenAIApiKey = "test-key";
            // No upstream endpoint set - CreateOpenAIProxy defaults to https://api.openai.com
            // so we test via UseProxy directly
        });

        // CreateOpenAIProxy sets a default upstream, so test UseProxy directly
        var simulator = this.CreateAPISimulator("TestAPI", options => { options.Port = 0; });
        var actDirect = () => simulator.UseProxy(new AIOptions { OpenAIApiKey = "test-key" }, "");

        actDirect.Should().Throw<ArgumentException>()
            .WithMessage("*Upstream URL*");
    }

    [Fact]
    public void ProxyMode_RequiresApiKey()
    {
        var simulator = this.CreateAPISimulator("TestAPI", options => { options.Port = 0; });
        var act = () => simulator.UseProxy(new AIOptions(), "https://api.openai.com");

        act.Should().Throw<ValidationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public void ProxyMode_RequiresValidUri()
    {
        var simulator = this.CreateAPISimulator("TestAPI", options => { options.Port = 0; });
        var act = () => simulator.UseProxy(new AIOptions { OpenAIApiKey = "test-key" }, "not-a-uri");

        act.Should().Throw<ValidationException>()
            .WithMessage("*valid absolute URI*");
    }

    [Fact]
    public void ProxyMode_ValidatesSuccessfully()
    {
        var simulator = this.CreateAPISimulator("TestAPI", options => { options.Port = 0; });
        var act = () => simulator.UseProxy(
            new AIOptions { OpenAIApiKey = "test-key" },
            "https://api.openai.com");

        act.Should().NotThrow();
    }

    [Fact]
    public void ProxyMode_CacheOnly_ShouldNotRequireApiKey()
    {
        // CacheOnly + proxy is a valid combination for CI replay
        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.Port = 0;
        });

        var act = () => simulator.UseProxy(new AIOptions(), "https://api.openai.com");

        act.Should().NotThrow();
    }

    [Fact]
    public void ProxyTimeoutSeconds_RejectsInvalidValues()
    {
        var options = new AIOptions();

        var act = () => options.ProxyTimeoutSeconds = 0;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProxyTimeoutSeconds_DefaultIs120()
    {
        var options = new AIOptions();

        options.ProxyTimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void CreateOpenAIProxy_SetsProxyMode()
    {
        var simulator = this.CreateOpenAIProxy(opt =>
        {
            opt.OpenAIApiKey = "test-key";
        });

        simulator.Should().NotBeNull();
    }

    [Fact]
    public void CreateAzureOpenAIProxy_SetsProxyMode()
    {
        var simulator = this.CreateAzureOpenAIProxy(opt =>
        {
            opt.OpenAIApiKey = "test-key";
            opt.OpenAIEndpoint = "https://my-resource.openai.azure.com";
            opt.AzureDeploymentName = "gpt-4";
        });

        simulator.Should().NotBeNull();
    }
}
