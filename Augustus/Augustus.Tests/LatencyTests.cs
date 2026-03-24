using System.Diagnostics;
using Augustus.Extensions;

namespace Augustus.Tests;

public class LatencyTests
{
    [Fact]
    public void LatencyOptions_NegativeMean_ShouldThrow()
    {
        var act = () => new LatencyOptions(-1, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LatencyOptions_NegativeStdDev_ShouldThrow()
    {
        var act = () => new LatencyOptions(100, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LatencyOptions_ValidValues_ShouldSetProperties()
    {
        var options = new LatencyOptions(150, 40);
        options.MeanMs.Should().Be(150);
        options.StdDevMs.Should().Be(40);
    }

    [Fact]
    public void GaussianLatency_ZeroStdDev_ShouldReturnMean()
    {
        var result = GaussianLatency.Sample(100, 0);
        result.Should().Be(100);
    }

    [Fact]
    public void GaussianLatency_ZeroMeanZeroStdDev_ShouldReturnZero()
    {
        var result = GaussianLatency.Sample(0, 0);
        result.Should().Be(0);
    }

    [Fact]
    public void GaussianLatency_ShouldClampNegativeValues()
    {
        // Use a seeded Random and large stdDev relative to mean to produce negative samples.
        // Try many seeds until we confirm at least one clamp occurs.
        var clamped = false;
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = GaussianLatency.Sample(10, 200, rng);
            result.Should().BeGreaterThanOrEqualTo(0, "delay must never be negative");
            if (result == 0) clamped = true;
        }

        clamped.Should().BeTrue("at least one sample should have been clamped to 0 with large stdDev");
    }

    [Fact]
    public void GaussianLatency_SeededRandom_ShouldClusterAroundMean()
    {
        var rng = new Random(42);
        var samples = new int[1000];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = GaussianLatency.Sample(150, 40, rng);

        var avg = samples.Average();
        avg.Should().BeApproximately(150, 10, "average of many samples should be close to the mean");
    }

    [Fact]
    public async Task Simulator_WithoutLatency_ShouldRespondQuickly()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { message = "fast" })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            var sw = Stopwatch.StartNew();
            var response = await client.GetStringAsync("/api/test");
            sw.Stop();

            response.Should().Contain("fast");
            sw.ElapsedMilliseconds.Should().BeLessThan(500, "no latency configured means near-instant response");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Simulator_WithLatency_ShouldDelayResponse()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .WithLatency(200, 0) // deterministic: exactly 200ms
            .ForGet("/api/test")
            .WithResponse(new { message = "delayed" })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            var sw = Stopwatch.StartNew();
            var response = await client.GetStringAsync("/api/test");
            sw.Stop();

            response.Should().Contain("delayed");
            sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(190, "response should be delayed by approximately the configured 200ms");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Simulator_WithLatency_ShouldComposeWithOtherOptions()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .WithLatency(50, 0)
            .ForGet("/api/customers/{id}")
            .WithResponse(new { id = "cus_123", name = "Test Customer" })
            .WithStatusCode(200)
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            var response = await client.GetAsync("/api/customers/cus_123");

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("cus_123");
            body.Should().Contain("Test Customer");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }
}
