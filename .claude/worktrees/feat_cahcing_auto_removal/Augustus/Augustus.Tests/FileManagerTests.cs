using FluentAssertions;
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
}
