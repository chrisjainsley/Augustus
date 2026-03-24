using System.Net;
using System.Text;
using System.Text.Json;
using Augustus.AI;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class ProxyCachingTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    private string CreateTempCacheDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"augustus_proxy_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private APISimulator CreateCacheOnlyProxySimulator(string cacheDir)
    {
        var sim = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.CacheFolderPath = cacheDir;
            options.Port = 0;
        });
        sim.UseProxy(new AIOptions(), "https://api.example.com");
        return sim;
    }

    private static async Task<string> ExtractHashFromProxyCacheMiss(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("requestHash").GetString()!;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public async Task ProxyCacheOnly_MultipleDistinctGetPaths_ProduceDifferentHashes()
    {
        var cacheDir = CreateTempCacheDir();
        await using var simulator = CreateCacheOnlyProxySimulator(cacheDir);
        await simulator.StartAsync();
        using var client = simulator.CreateClient();

        var response1 = await client.GetAsync("/repos/owner/repo/git/trees/HEAD?recursive=1");
        var hash1 = await ExtractHashFromProxyCacheMiss(response1);

        var response2 = await client.GetAsync("/repos/owner/repo/git/blobs/abc123");
        var hash2 = await ExtractHashFromProxyCacheMiss(response2);

        hash1.Should().NotBe(hash2, "GET requests to different paths should produce different cache keys");
    }

    [Fact]
    public async Task ProxyCacheOnly_SameGetPath_DifferentQueryStrings_ProduceDifferentHashes()
    {
        var cacheDir = CreateTempCacheDir();
        await using var simulator = CreateCacheOnlyProxySimulator(cacheDir);
        await simulator.StartAsync();
        using var client = simulator.CreateClient();

        var response1 = await client.GetAsync("/api/search?q=foo");
        var hash1 = await ExtractHashFromProxyCacheMiss(response1);

        var response2 = await client.GetAsync("/api/search?q=bar");
        var hash2 = await ExtractHashFromProxyCacheMiss(response2);

        hash1.Should().NotBe(hash2, "GET requests with different query strings should produce different cache keys");
    }

    [Fact]
    public async Task ProxyCacheOnly_GetVsPost_SamePath_ProduceDifferentHashes()
    {
        var cacheDir = CreateTempCacheDir();
        await using var simulator = CreateCacheOnlyProxySimulator(cacheDir);
        await simulator.StartAsync();
        using var client = simulator.CreateClient();

        var getResponse = await client.GetAsync("/api/resource");
        var getHash = await ExtractHashFromProxyCacheMiss(getResponse);

        var postResponse = await client.PostAsync("/api/resource", new StringContent("{}", Encoding.UTF8, "application/json"));
        var postHash = await ExtractHashFromProxyCacheMiss(postResponse);

        getHash.Should().NotBe(postHash, "GET and POST to the same path should produce different cache keys");
    }

    [Fact]
    public async Task ProxyCacheOnly_GetRequest_CacheHitServesCorrectResponse()
    {
        var cacheDir = CreateTempCacheDir();
        await using var simulator = CreateCacheOnlyProxySimulator(cacheDir);
        await simulator.StartAsync();
        using var client = simulator.CreateClient();

        // First request: cache miss — extract hash
        var missResponse = await client.GetAsync("/api/users/1");
        var requestHash = await ExtractHashFromProxyCacheMiss(missResponse);

        // Pre-populate cache using the same FileManager the simulator uses
        var fileManager = new APISimulator.FileManager(cacheDir);
        await fileManager.CacheResponseAsync(requestHash,
            "{\"id\":1,\"login\":\"octocat\"}", "PROXY GET /api/users/1", new List<string>());

        // Second request: cache hit
        var hitResponse = await client.GetAsync("/api/users/1");
        hitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var hitBody = await hitResponse.Content.ReadAsStringAsync();
        hitBody.Should().Contain("octocat");
    }
}
