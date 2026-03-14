using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.Tests;

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
        var cacheDir = CreateTempCacheDir();

        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.CacheFolderPath = cacheDir;
            options.Port = 0;
        })
        .WithInstruction("Return test responses");

        await using (simulator)
        {
            await simulator.StartAsync();
            var client = simulator.CreateClient();

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

        var simulator = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.CacheFolderPath = cacheDir;
            options.Port = 0;
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
