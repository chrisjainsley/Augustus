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

        using var response1 = await client.GetAsync("/repos/owner/repo/git/trees/HEAD?recursive=1");
        var hash1 = await ExtractHashFromProxyCacheMiss(response1);

        using var response2 = await client.GetAsync("/repos/owner/repo/git/blobs/abc123");
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

        using var response1 = await client.GetAsync("/api/search?q=foo");
        var hash1 = await ExtractHashFromProxyCacheMiss(response1);

        using var response2 = await client.GetAsync("/api/search?q=bar");
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

        using var getResponse = await client.GetAsync("/api/resource");
        var getHash = await ExtractHashFromProxyCacheMiss(getResponse);

        using var postResponse = await client.PostAsync("/api/resource", new StringContent("{}", Encoding.UTF8, "application/json"));
        var postHash = await ExtractHashFromProxyCacheMiss(postResponse);

        getHash.Should().NotBe(postHash, "GET and POST to the same path should produce different cache keys");
    }

    [Fact]
    public async Task ProxyCacheOnly_RenamedFixture_IsServedByContentMatch()
    {
        var cacheDir = CreateTempCacheDir();

        // A renamed fixture committed before the run (proxy uses no instructions, so the
        // canonical request is fully deterministic from the request line).
        var keyResult = global::Augustus.RequestKeyFactory.Create(
            "GET", "/api/widgets/42", "?expand=true", Array.Empty<byte>(), null, null);
        var fileManager = new APISimulator.FileManager(cacheDir);
        await fileManager.CacheResponseAsync(keyResult.Hash, "{\"id\":42,\"name\":\"widget\"}",
            "PROXY GET /api/widgets/42", new List<string>(), normalized: false, keyResult.Canonical);
        File.Move(
            Path.Combine(cacheDir, $"{keyResult.Hash}.json"),
            Path.Combine(cacheDir, "get-widget-42.json"));

        await using var simulator = CreateCacheOnlyProxySimulator(cacheDir);
        await simulator.StartAsync();
        using var client = simulator.CreateClient();

        using var hitResponse = await client.GetAsync("/api/widgets/42?expand=true");
        hitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await hitResponse.Content.ReadAsStringAsync()).Should().Contain("widget");
    }

    [Fact]
    public async Task ProxyCacheOnly_AzureDeploymentRename_StillServedWhenNormalizationEnabled()
    {
        var cacheDir = CreateTempCacheDir();

        // Fixture recorded under the original deployment name, NO normalization.
        var legacy = global::Augustus.RequestKeyFactory.Create(
            "POST", "/openai/deployments/gpt4-prod/chat/completions", "?api-version=2024-06-01",
            System.Text.Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}"), null, null);
        var fm = new APISimulator.FileManager(cacheDir);
        await fm.CacheResponseAsync(legacy.Hash, "{\"id\":\"azure-ok\"}", "PROXY",
            new List<string>(), normalized: false, legacy.Canonical);

        var sim = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.CacheFolderPath = cacheDir;
            options.Port = 0;
            options.NormalizeAzureOpenAIDeployment = true;
        });
        sim.UseProxy(new AIOptions(), "https://example.openai.azure.com");

        await using (sim)
        {
            await sim.StartAsync();
            using var client = sim.CreateClient();

            // Upstream deployment was renamed/migrated — different segment, same prompt.
            using var response = await client.PostAsync(
                "/openai/deployments/gpt4-prod-eastus2/chat/completions?api-version=2024-06-01",
                new StringContent("{\"model\":\"gpt-4\"}", System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("azure-ok");
        }
    }

    [Fact]
    public async Task ProxyCacheOnly_CacheMiss_FiresHookAndExtends503Payload()
    {
        var cacheDir = CreateTempCacheDir();
        global::Augustus.CacheMissDiagnostic? captured = null;
        var sim = this.CreateAPISimulator("TestAPI", options =>
        {
            options.CacheOnly = true;
            options.CacheFolderPath = cacheDir;
            options.Port = 0;
            options.OnCacheMiss = d => captured = d;
        });
        sim.UseProxy(new AIOptions(), "https://api.example.com");

        await using (sim)
        {
            await sim.StartAsync();
            using var client = sim.CreateClient();

            using var response = await client.GetAsync("/api/things/7?detail=full");
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Existing fields preserved.
            root.GetProperty("status").GetInt32().Should().Be(503);
            var requestHash = root.GetProperty("requestHash").GetString()!;
            // New diagnostic field.
            var canonical = root.GetProperty("expectedCanonicalRequest");
            canonical.GetProperty("Path").GetString().Should().Be("/api/things/7");
            canonical.GetProperty("Method").GetString().Should().Be("GET");

            // Hook fired with the same identity.
            captured.Should().NotBeNull();
            captured!.ComputedKey.Should().Be(requestHash);
            captured.ExpectedCanonicalRequest.Path.Should().Be("/api/things/7");
            captured.CachePath.Should().Be(cacheDir);
        }
    }

    [Fact]
    public async Task ProxyCacheOnly_GetRequest_CacheHitServesCorrectResponse()
    {
        var cacheDir = CreateTempCacheDir();
        await using var simulator = CreateCacheOnlyProxySimulator(cacheDir);
        await simulator.StartAsync();
        using var client = simulator.CreateClient();

        // First request: cache miss — extract hash
        using var missResponse = await client.GetAsync("/api/users/1");
        var requestHash = await ExtractHashFromProxyCacheMiss(missResponse);

        // Pre-populate cache using the same FileManager the simulator uses
        var fileManager = new APISimulator.FileManager(cacheDir);
        await fileManager.CacheResponseAsync(requestHash,
            "{\"id\":1,\"login\":\"octocat\"}", "PROXY GET /api/users/1", new List<string>());

        // Second request: cache hit
        using var hitResponse = await client.GetAsync("/api/users/1");
        hitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var hitBody = await hitResponse.Content.ReadAsStringAsync();
        hitBody.Should().Contain("octocat");
    }
}
