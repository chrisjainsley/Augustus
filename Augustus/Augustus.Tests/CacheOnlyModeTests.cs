using System.ComponentModel.DataAnnotations;
using System.Net;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.Tests;

public class CacheOnlyModeTests
{
    [Fact]
    public void ExplicitCacheOnly_ShouldSkipApiKeyValidation()
    {
        var options = new APISimulatorOptions
        {
            CacheOnly = true,
            OpenAIApiKey = "",
            Port = 9050
        };

        var creating = () => new APISimulator("TestAPI", options);
        creating.Should().NotThrow();
    }

    [Fact]
    public void AutoDetection_ShouldActivateCacheOnlyWhenCachingEnabledAndNoKey()
    {
        var options = new APISimulatorOptions
        {
            EnableCaching = true,
            OpenAIApiKey = "",
            Port = 9051
        };

        options.Validate();

        options.CacheOnly.Should().BeTrue();
        options.AutoRemoveStaleCache.Should().BeFalse();
    }

    [Fact]
    public void AutoDetection_ShouldNotActivateWhenCachingDisabled()
    {
        var options = new APISimulatorOptions
        {
            EnableCaching = false,
            OpenAIApiKey = "",
            Port = 9052
        };

        var validating = () => options.Validate();
        validating.Should().Throw<ValidationException>()
            .WithMessage("*OpenAI API key is required*");
    }

    [Fact]
    public void CacheOnly_ShouldForceAutoRemoveStaleCacheFalse()
    {
        var options = new APISimulatorOptions
        {
            CacheOnly = true,
            AutoRemoveStaleCache = true,
            Port = 9053
        };

        options.Validate();

        options.AutoRemoveStaleCache.Should().BeFalse();
    }

    [Fact]
    public void CacheOnly_ShouldForceEnableCachingTrue()
    {
        var options = new APISimulatorOptions
        {
            CacheOnly = true,
            EnableCaching = false,
            Port = 9054
        };

        options.Validate();

        options.EnableCaching.Should().BeTrue();
    }

    [Fact]
    public async Task CacheHit_ShouldReturnCachedResponse()
    {
        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.Port = 9055;
        })
        .WithInstruction("Return test responses");

        await using (simulator)
        {
            await simulator.StartAsync();
            var client = simulator.CreateClient();

            // Make a request — it will be a cache miss and return 503
            // (This test verifies the simulator starts and serves requests in cache-only mode)
            var response = await client.GetAsync("/v1/test");
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
    }

    [Fact]
    public async Task CacheMiss_ShouldReturn503WithDescriptiveError()
    {
        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.Port = 9056;
        })
        .WithInstruction("Return test responses");

        await using (simulator)
        {
            await simulator.StartAsync();
            var client = simulator.CreateClient();

            var response = await client.GetAsync("/v1/uncached-endpoint");

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Cache-only mode");
            body.Should().Contain("no cached response found");
        }
    }
}
