using FluentAssertions;
using System.Text;
using System.Text.Json;

namespace Augustus.Tests;

public class FileManagerTests
{
    [Fact]
    public async Task CacheResponseAsync_ShouldRedactSensitiveValuesFromSavedMetadata()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            var requestHash = "ABCDEF";
            var response = "{\"ok\":true}";
            var originalRequest = "curl -X POST -H \"Authorization: Bearer super-secret-token\" -H \"x-api-key: key-123\" -d '{\"access_token\":\"token-456\",\"password\":\"p@ss\"}' \"https://example.com/v1/items?api_key=qwerty\"";
            var instructions = new List<string>
            {
                "Use token=abc123 for auth in examples",
                "Never return client_secret: xyz"
            };

            await fileManager.CacheResponseAsync(requestHash, response, originalRequest, instructions);
            var json = await fileManager.ReadFromFileAsync($"{requestHash}.json");

            json.Should().NotBeNull();
            json.Should().Contain("[REDACTED]");
            json.Should().NotContain("super-secret-token");
            json.Should().NotContain("key-123");
            json.Should().NotContain("token-456");
            json.Should().NotContain("qwerty");
            json.Should().NotContain("xyz");
            json.Should().NotContain("p@ss");
        }
        finally
        {
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReadCachedResponseAsync_ShouldStillReturnResponse_WhenMetadataIsRedacted()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            var requestHash = "123456";
            var expectedResponse = "{\"result\":\"cached\"}";

            await fileManager.CacheResponseAsync(
                requestHash,
                expectedResponse,
                "curl -H \"Authorization: Bearer abc\" \"https://example.com?token=123\"",
                new List<string> { "Use api_key: my-key" });

            var cached = await fileManager.ReadCachedResponseAsync(requestHash);
            cached.Should().Be(expectedResponse);
        }
        finally
        {
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RemoveStaleEntries_ShouldDeleteUntouchedFiles()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);

            // Create 3 cache files via CacheResponseAsync
            await fileManager.CacheResponseAsync("HASH_A", "{\"a\":1}", "curl a", new List<string> { "inst" });
            await fileManager.CacheResponseAsync("HASH_B", "{\"b\":2}", "curl b", new List<string> { "inst" });
            await fileManager.CacheResponseAsync("HASH_C", "{\"c\":3}", "curl c", new List<string> { "inst" });

            // Create a new FileManager to reset touch tracking
            var fileManager2 = new APISimulator.FileManager(cachePath);

            // Read 2 of 3 (touches them)
            var responseA = await fileManager2.ReadCachedResponseAsync("HASH_A");
            var responseB = await fileManager2.ReadCachedResponseAsync("HASH_B");
            responseA.Should().NotBeNull();
            responseB.Should().NotBeNull();

            // Remove stale entries
            fileManager2.RemoveStaleEntries();

            // HASH_A and HASH_B should remain, HASH_C should be deleted
            File.Exists(Path.Combine(cachePath, "HASH_A.json")).Should().BeTrue();
            File.Exists(Path.Combine(cachePath, "HASH_B.json")).Should().BeTrue();
            File.Exists(Path.Combine(cachePath, "HASH_C.json")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public async Task RemoveStaleEntries_ShouldKeepNewlyWrittenFiles()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);

            // Write a cache entry via CacheResponseAsync (auto-touched)
            await fileManager.CacheResponseAsync("WRITTEN", "{\"ok\":true}", "curl x", new List<string> { "inst" });

            // Write a stale .json file directly (bypassing FileManager, so not touched)
            await File.WriteAllTextAsync(Path.Combine(cachePath, "STALE.json"), "{}");

            fileManager.RemoveStaleEntries();

            File.Exists(Path.Combine(cachePath, "WRITTEN.json")).Should().BeTrue();
            File.Exists(Path.Combine(cachePath, "STALE.json")).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public void RemoveStaleEntries_ShouldHandleEmptyFolder()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            var act = () => fileManager.RemoveStaleEntries();
            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public async Task ReadCachedEntryAsync_NormalizedTrue_RoundTripsThoughDisk()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            const string hash = "NORM_TRUE";

            await fileManager.CacheResponseAsync(hash, "{\"ok\":true}", "curl ...", new List<string> { "inst" }, normalized: true);
            var entry = await fileManager.ReadCachedEntryAsync(hash);

            entry.Should().NotBeNull();
            entry!.Normalized.Should().BeTrue();
            entry.Response.Should().Be("{\"ok\":true}");
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public async Task ReadCachedEntryAsync_DefaultNormalized_ReturnsFalse()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            const string hash = "NORM_DEFAULT";

            // Default overload omits normalized: true, so Normalized should be false
            await fileManager.CacheResponseAsync(hash, "{\"ok\":true}", "curl ...", new List<string> { "inst" });
            var entry = await fileManager.ReadCachedEntryAsync(hash);

            entry.Should().NotBeNull();
            entry!.Normalized.Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public async Task ReadCachedEntryAsync_LegacyFileWithoutNormalizedField_DefaultsToFalse()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            const string hash = "LEGACY_NORM";

            // Simulate a pre-existing cache file that predates the Normalized field
            var legacyJson = "{\"RequestHash\":\"LEGACY_NORM\",\"Response\":\"{\\\"ok\\\":true}\",\"OriginalRequest\":\"curl ...\",\"Instructions\":[],\"Timestamp\":\"2024-01-01T00:00:00Z\"}";
            await File.WriteAllTextAsync(Path.Combine(cachePath, $"{hash}.json"), legacyJson);

            var entry = await fileManager.ReadCachedEntryAsync(hash);

            entry.Should().NotBeNull();
            entry!.Normalized.Should().BeFalse();
            entry.Response.Should().Be("{\"ok\":true}");
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public void RemoveStaleEntries_ShouldHandleNonexistentFolder()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        var fileManager = new APISimulator.FileManager(cachePath);

        // Delete the folder after construction
        Directory.Delete(cachePath, recursive: true);

        var act = () => fileManager.RemoveStaleEntries();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task CacheResponseAsync_PersistsCanonicalRequest()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            var canonical = new CanonicalRequest(
                "POST", "/v1/chat/completions", "?api-version=2024-06-01",
                new[] { "be terse" }, "{\"model\":\"gpt-4\"}");

            await fileManager.CacheResponseAsync("ABCDEF", "{\"ok\":true}", "curl ...",
                new List<string> { "be terse" }, normalized: true, canonical: canonical);

            var entry = await fileManager.ReadCachedEntryAsync("ABCDEF");
            entry.Should().NotBeNull();
            entry!.CanonicalRequest.Should().NotBeNull();
            entry.CanonicalRequest!.Method.Should().Be("POST");
            entry.CanonicalRequest.Path.Should().Be("/v1/chat/completions");
            entry.CanonicalRequest.NormalizedBody.Should().Be("{\"model\":\"gpt-4\"}");
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public async Task ReadCachedEntryAsync_LegacyFileWithoutCanonicalRequest_StillDeserializes()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            // Pre-#106 on-disk shape: no CanonicalRequest field.
            var legacyJson =
                "{\"RequestHash\":\"DEADBEEF\",\"Response\":\"{\\\"ok\\\":true}\"," +
                "\"OriginalRequest\":\"curl ...\",\"Instructions\":[],\"Timestamp\":\"2024-01-01T00:00:00Z\"," +
                "\"Normalized\":true}";
            await File.WriteAllTextAsync(Path.Combine(cachePath, "DEADBEEF.json"), legacyJson);

            var fileManager = new APISimulator.FileManager(cachePath);
            var entry = await fileManager.ReadCachedEntryAsync("DEADBEEF");

            entry.Should().NotBeNull();
            entry!.Response.Should().Be("{\"ok\":true}");
            entry.CanonicalRequest.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    private static readonly Func<CanonicalRequest, string> Recompute =
        c => RequestKeyFactory.ComputeKeyFromCanonical(c, null);

    private static (string key, CanonicalRequest canonical) BuildKey(
        string method, string path, string? query, byte[] body)
    {
        var result = RequestKeyFactory.Create(method, path, query, body, null, null);
        return (result.Hash, result.Canonical);
    }

    [Fact]
    public async Task ResolveEntryAsync_FilenameEqualsHash_ResolvesViaFastPath()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
            var (key, canonical) = BuildKey("POST", "/v1/chat/completions", null, body);
            var fm = new APISimulator.FileManager(cachePath);
            await fm.CacheResponseAsync(key, "{\"ok\":1}", "curl", new List<string>(), true, canonical);

            var entry = await fm.ResolveEntryAsync(key, Recompute);

            entry.Should().NotBeNull();
            entry!.Response.Should().Be("{\"ok\":1}");
        }
        finally { Directory.Delete(cachePath, true); }
    }

    [Fact]
    public async Task ResolveEntryAsync_RenamedFile_ResolvesByCanonicalRequest()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
            var (key, canonical) = BuildKey("POST", "/v1/chat/completions", null, body);
            var fm = new APISimulator.FileManager(cachePath);
            await fm.CacheResponseAsync(key, "{\"ok\":2}", "curl", new List<string>(), true, canonical);

            // Rename the fixture to a human-friendly label.
            File.Move(
                Path.Combine(cachePath, $"{key}.json"),
                Path.Combine(cachePath, "happy-path-chat.json"));

            // Fresh manager so no in-memory state remembers the original name.
            var fm2 = new APISimulator.FileManager(cachePath);
            var entry = await fm2.ResolveEntryAsync(key, Recompute);

            entry.Should().NotBeNull();
            entry!.Response.Should().Be("{\"ok\":2}");
        }
        finally { Directory.Delete(cachePath, true); }
    }

    [Fact]
    public async Task ResolveEntryAsync_HandAuthoredFile_ResolvesByCanonicalRequest()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
            var (key, canonical) = BuildKey("POST", "/v1/chat/completions", null, body);

            // Author a fixture entirely by hand under an arbitrary name.
            var handAuthored = new
            {
                RequestHash = "ignored",
                Response = "{\"ok\":\"hand\"}",
                OriginalRequest = "",
                Instructions = Array.Empty<string>(),
                Timestamp = DateTime.UtcNow,
                Normalized = true,
                CanonicalRequest = canonical
            };
            await File.WriteAllTextAsync(
                Path.Combine(cachePath, "authored-by-a-human.json"),
                JsonSerializer.Serialize(handAuthored));

            var fm = new APISimulator.FileManager(cachePath);
            var entry = await fm.ResolveEntryAsync(key, Recompute);

            entry.Should().NotBeNull();
            entry!.Response.Should().Be("{\"ok\":\"hand\"}");
        }
        finally { Directory.Delete(cachePath, true); }
    }

    [Fact]
    public async Task RemoveStaleEntries_KeepsContentMatchedRenamedFile()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
            var (key, canonical) = BuildKey("POST", "/v1/chat/completions", null, body);
            var fm = new APISimulator.FileManager(cachePath);
            await fm.CacheResponseAsync(key, "{\"ok\":3}", "curl", new List<string>(), true, canonical);
            File.Move(
                Path.Combine(cachePath, $"{key}.json"),
                Path.Combine(cachePath, "renamed.json"));

            var fm2 = new APISimulator.FileManager(cachePath);
            (await fm2.ResolveEntryAsync(key, Recompute)).Should().NotBeNull();
            fm2.RemoveStaleEntries();

            File.Exists(Path.Combine(cachePath, "renamed.json")).Should().BeTrue();
        }
        finally { Directory.Delete(cachePath, true); }
    }

    [Fact]
    public async Task ResolveEntryAsync_LegacyFileNoCanonicalRequest_ResolvesByFilename()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-cache-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var legacyJson =
                "{\"RequestHash\":\"LEGACYKEY\",\"Response\":\"{\\\"ok\\\":\\\"legacy\\\"}\"," +
                "\"OriginalRequest\":\"\",\"Instructions\":[],\"Timestamp\":\"2024-01-01T00:00:00Z\"," +
                "\"Normalized\":true}";
            await File.WriteAllTextAsync(Path.Combine(cachePath, "LEGACYKEY.json"), legacyJson);

            var fm = new APISimulator.FileManager(cachePath);
            var entry = await fm.ResolveEntryAsync("LEGACYKEY", Recompute);

            entry.Should().NotBeNull();
            entry!.Response.Should().Be("{\"ok\":\"legacy\"}");
        }
        finally { Directory.Delete(cachePath, true); }
    }
}
