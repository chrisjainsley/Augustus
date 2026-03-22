using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Augustus;
using Augustus.AI;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class OpenAIOptimizationTests
{
    [Fact]
    public void DynamicContentFieldsMerger_MergesGlobalAndRoute_WithoutDuplicates()
    {
        var global = new[] { "a", "b" };
        var route = new[] { "b", "c" };

        var merged = DynamicContentFieldsMerger.Merge(global, route);

        merged.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public void DynamicContentFieldsMerger_RouteOnly_ReturnsRoute()
    {
        var route = new[] { "x" };
        DynamicContentFieldsMerger.Merge(null, route).Should().BeSameAs(route);
    }

    [Fact]
    public void OpenAIRetryDelays_ProxyRetryAfterDelta_IsHonored()
    {
        using var response = new HttpResponseMessage((HttpStatusCode)429);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2));

        var delay = OpenAIRetryDelays.ComputeProxyRetryDelayMs(
            new AIOptions { OpenAIApiKey = "k", InitialRetryDelayMs = 100, MaxRetryDelayMs = 60_000 },
            currentBackoffMs: 100,
            response);

        delay.Should().BeGreaterThanOrEqualTo(2000);
    }

    [Fact]
    public void OpenAICallCoordinator_InstanceKey_IncludesConcurrencySoLimitsStayIndependent()
    {
        var low = new AIOptions { OpenAIApiKey = "same-key", MaxConcurrentRequests = 1 };
        var high = new AIOptions { OpenAIApiKey = "same-key", MaxConcurrentRequests = 10 };

        OpenAICallCoordinator.BuildInstanceKey(low).Should().NotBe(OpenAICallCoordinator.BuildInstanceKey(high));
    }

    [Fact]
    public async Task RouteLevel_UseAI_CacheOnly_CacheMiss_Returns503_WithoutApiKey()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"aug_route_co_{Guid.NewGuid():N}");
        Directory.CreateDirectory(cacheDir);
        try
        {
            var simulator = this.CreateAPISimulator("TestAPI", options =>
            {
                options.CacheOnly = true;
                options.CacheFolderPath = cacheDir;
                options.Port = 0;
            });
            simulator
                .ForPost("/route/items")
                .UseAI(new AIOptions(), "Return minimal JSON")
                .Add();

            await using (simulator)
            {
                await simulator.StartAsync();
                using var client = simulator.CreateClient();
                using var response = await client.PostAsync(
                    "/route/items",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(cacheDir, true);
            }
            catch
            {
            }
        }
    }
}
