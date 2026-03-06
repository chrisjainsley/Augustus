using FluentAssertions;

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
}
