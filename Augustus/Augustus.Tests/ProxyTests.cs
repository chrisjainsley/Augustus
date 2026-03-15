using System.ComponentModel.DataAnnotations;
using System.Text;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.Tests;

public class ProxyTests
{
    [Fact]
    public void ProxyMode_CacheOnly_ShouldNotRequireApiKey()
    {
        // ProxyMode + CacheOnly is a valid combination for CI replay
        var creating = () => this.CreateAzureOpenAIProxy(options =>
        {
            options.CacheOnly = true;
            options.Port = 9050;
        });

        creating.Should().NotThrow();
    }

    [Fact]
    public void ProxyMode_WithoutEndpoint_ShouldThrow()
    {
        var creating = () => this.CreateAzureOpenAIProxy(options =>
        {
            options.OpenAIEndpoint = "";
            options.Port = 9051;
        });

        creating.Should().Throw<ValidationException>()
            .WithMessage("*endpoint*required*proxy*");
    }

    [Fact]
    public void ProxyMode_WithEndpoint_ShouldSucceed()
    {
        var creating = () => this.CreateAzureOpenAIProxy(options =>
        {
            options.OpenAIEndpoint = "https://my-resource.openai.azure.com";
            options.Port = 9052;
        });

        creating.Should().NotThrow();
    }

    [Fact]
    public void CacheKeyComputer_SameInputs_ProducesSameHash()
    {
        var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\",\"messages\":[]}");
        var hash1 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body);
        var hash2 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CacheKeyComputer_WithoutInstructions_DiffersFromWithInstructions()
    {
        var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
        var hashNoInstructions = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body);
        var hashWithInstructions = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body,
            new List<string> { "Return realistic responses" });

        hashNoInstructions.Should().NotBe(hashWithInstructions);
    }

    [Fact]
    public void CacheKeyComputer_ProxyAndSimulatorCacheOnly_SameHashWithoutInstructions()
    {
        // This is the key scenario: proxy writes cache with no instructions,
        // simulator in CacheOnly mode (no instructions) should produce the same hash.
        var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"hello\"}]}");

        // Proxy hash (no instructions)
        var proxyHash = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", "?api-version=2024-06-01", body);

        // Simulator CacheOnly hash (no instructions = same as proxy)
        var simulatorHash = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", "?api-version=2024-06-01", body);

        proxyHash.Should().Be(simulatorHash);
    }

    [Fact]
    public async Task ProxyCacheFile_ReadableBySimulatorCacheOnly_RoundTrip()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"augustus_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(cacheDir);

            // Simulate what the proxy writes
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"test\"}]}");
            var hash = CacheKeyComputer.ComputeCacheKey("POST", "/openai/deployments/gpt-4/chat/completions", "?api-version=2024-06-01", body);

            var fileManager = new APISimulator.FileManager(cacheDir);
            var expectedResponse = "{\"id\":\"chatcmpl-abc\",\"choices\":[{\"message\":{\"content\":\"hello\"}}]}";
            await fileManager.CacheResponseAsync(hash, expectedResponse, "curl ...", new List<string>());

            // Simulate what the simulator reads in CacheOnly mode (same hash, no instructions)
            var readHash = CacheKeyComputer.ComputeCacheKey("POST", "/openai/deployments/gpt-4/chat/completions", "?api-version=2024-06-01", body);
            readHash.Should().Be(hash);

            var cached = await fileManager.ReadCachedResponseAsync(readHash);
            cached.Should().Be(expectedResponse);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
                Directory.Delete(cacheDir, true);
        }
    }

    [Fact]
    public async Task SimulatorCacheFile_WithInstructions_NotReadableByProxy()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"augustus_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(cacheDir);

            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");

            // Simulator writes with instructions
            var simulatorHash = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body,
                new List<string> { "You are a Stripe API simulator" });

            var fileManager = new APISimulator.FileManager(cacheDir);
            await fileManager.CacheResponseAsync(simulatorHash, "{\"response\":\"sim\"}", "curl ...",
                new List<string> { "You are a Stripe API simulator" });

            // Proxy tries to read (no instructions) — different hash
            var proxyHash = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body);
            proxyHash.Should().NotBe(simulatorHash);

            var cached = await fileManager.ReadCachedResponseAsync(proxyHash);
            cached.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(cacheDir))
                Directory.Delete(cacheDir, true);
        }
    }

    [Fact]
    public void CacheKeyComputer_DifferentMethods_ProduceDifferentHashes()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        var getHash = CacheKeyComputer.ComputeCacheKey("GET", "/v1/models", null, body);
        var postHash = CacheKeyComputer.ComputeCacheKey("POST", "/v1/models", null, body);

        getHash.Should().NotBe(postHash);
    }

    [Fact]
    public void CacheKeyComputer_DifferentQueryStrings_ProduceDifferentHashes()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        var hash1 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", "?api-version=2024-06-01", body);
        var hash2 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", "?api-version=2024-02-01", body);

        hash1.Should().NotBe(hash2);
    }
}
