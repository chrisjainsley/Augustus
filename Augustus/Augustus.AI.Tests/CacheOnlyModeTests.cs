using System.Net;
using System.Text.Json;
using Augustus.AI;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class CacheOnlyModeTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    private string CreateTempCacheDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"augustus_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private APISimulator CreateCacheOnlySimulator(string cacheDir)
    {
        var sim = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.CacheFolderPath = cacheDir;
            options.Port = 0;
        });
        sim.UseAI(new AIOptions());
        sim.AddInstruction("Return test responses");
        return sim;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void ExplicitCacheOnly_ShouldSkipApiKeyValidation()
    {
        // When CacheOnly is true, UseAI should not require an API key
        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.Port = 0;
        });

        var act = () => simulator.UseAI(new AIOptions { OpenAIApiKey = "" });
        act.Should().NotThrow();
    }

    [Fact]
    public async Task CacheHit_ShouldReturnCachedResponse()
    {
        var cacheDir = CreateTempCacheDir();
        var simulator = CreateCacheOnlySimulator(cacheDir);

        await using (simulator)
        {
            await simulator.StartAsync();
            using var client = simulator.CreateClient();

            // First request: cache miss — extract the hash from the 503 error body
            var missResponse = await client.GetAsync("/v1/test");
            missResponse.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var missBody = await missResponse.Content.ReadAsStringAsync();
            var hashMatch = System.Text.RegularExpressions.Regex.Match(missBody, @"request hash (?:'|\\u0027)([A-F0-9]+)(?:'|\\u0027)");
            hashMatch.Success.Should().BeTrue("error response should contain the request hash");
            var requestHash = hashMatch.Groups[1].Value;

            // Pre-populate the cache with a response for that hash
            var cacheEntry = JsonSerializer.Serialize(new
            {
                RequestHash = requestHash,
                Response = "{\"id\":\"test-123\",\"status\":\"ok\"}",
                OriginalRequest = "GET /v1/test",
                Instructions = new[] { "Return test responses" },
                Timestamp = DateTime.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(cacheDir, $"{requestHash}.json"), cacheEntry);

            // Second request: should hit the cache and return 200
            var hitResponse = await client.GetAsync("/v1/test");
            hitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var hitBody = await hitResponse.Content.ReadAsStringAsync();
            hitBody.Should().Contain("test-123");
        }
    }

    [Fact]
    public async Task CacheMiss_ShouldReturn503WithDescriptiveError()
    {
        var cacheDir = CreateTempCacheDir();
        var simulator = CreateCacheOnlySimulator(cacheDir);

        await using (simulator)
        {
            await simulator.StartAsync();
            using var client = simulator.CreateClient();

            var response = await client.GetAsync("/v1/uncached-endpoint");

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Cache-only mode");
            body.Should().Contain("no cached response found");
        }
    }
}
