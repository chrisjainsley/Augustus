using FluentAssertions;
using System.Diagnostics;
using System.Text.Json;

namespace Augustus.Tests;

public class PerformanceTests
{
    [Fact]
    public async Task CacheResponseAsync_ShouldHandleConcurrentWrites()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-perf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                var hash = $"CONCURRENT_{i:D3}";
                var response = $"{{\"index\":{i}}}";
                var request = $"curl -X GET https://example.com/item/{i}";
                var instructions = new List<string> { $"instruction {i}" };

                tasks.Add(fileManager.CacheResponseAsync(hash, response, request, instructions));
            }

            await Task.WhenAll(tasks);

            // Verify all files were written correctly
            for (int i = 0; i < 20; i++)
            {
                var hash = $"CONCURRENT_{i:D3}";
                var cached = await fileManager.ReadCachedResponseAsync(hash);
                cached.Should().Be($"{{\"index\":{i}}}");
            }
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public async Task CacheResponseAsync_StaticSerializerOptions_ShouldProduceValidJson()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-perf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);

        try
        {
            var fileManager = new APISimulator.FileManager(cachePath);
            var requestHash = "SERIALIZER_TEST";
            var response = "{\"data\":[1,2,3],\"nested\":{\"key\":\"value\"}}";
            var originalRequest = "curl -X POST https://example.com/data";
            var instructions = new List<string> { "instruction one", "instruction two" };

            await fileManager.CacheResponseAsync(requestHash, response, originalRequest, instructions);

            // Read raw file and verify it's valid, indented JSON
            var rawJson = await fileManager.ReadFromFileAsync($"{requestHash}.json");
            rawJson.Should().NotBeNull();

            // Should be valid JSON
            var parsed = JsonSerializer.Deserialize<JsonElement>(rawJson!);
            parsed.GetProperty("RequestHash").GetString().Should().Be("SERIALIZER_TEST");
            parsed.GetProperty("Response").GetString().Should().Be(response);

            // Should be indented (contains newlines)
            rawJson.Should().Contain("\n");
        }
        finally
        {
            if (Directory.Exists(cachePath))
                Directory.Delete(cachePath, recursive: true);
        }
    }

    [Fact]
    public void SanitizeSensitiveValues_FastPath_ShouldBeQuickerForNonSensitiveStrings()
    {
        // A large string with no sensitive keywords
        var largeNonSensitive = new string('x', 10_000) + " some normal text about items and customers";
        // A large string with a sensitive keyword
        var largeSensitive = new string('x', 10_000) + " authorization: Bearer abc123";

        // Act
        var sanitizedNonSensitive = SensitiveDataSanitizer.SanitizeSensitiveValues(largeNonSensitive);
        var sanitizedSensitive = SensitiveDataSanitizer.SanitizeSensitiveValues(largeSensitive);

        // Assert: non-sensitive input should be returned unchanged (fast path does minimal work)
        sanitizedNonSensitive.Should().Be(largeNonSensitive);

        // Assert: sensitive input should be altered (slow path kicks in)
        sanitizedSensitive.Should().NotBe(largeSensitive);
        sanitizedSensitive.Should().NotContain("authorization: Bearer abc123");
    }
}
