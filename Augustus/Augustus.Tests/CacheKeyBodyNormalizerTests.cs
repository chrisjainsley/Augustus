using System.Text;
using Augustus;
using FluentAssertions;

namespace Augustus.Tests;

public class CacheKeyBodyNormalizerTests
{
    [Fact]
    public void NormalizeForCacheKey_NoProperties_JSON_IsCanonicalized()
    {
        var body = Encoding.UTF8.GetBytes("{\"id\":\"abc\",\"value\":42}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, Array.Empty<string>());

        // JSON bodies are re-serialized (sorted keys, stable escaping) even with no dynamic fields.
        result.Should().NotBeSameAs(body);
        Encoding.UTF8.GetString(result).Should().Be("{\"id\":\"abc\",\"value\":42}");
    }

    [Fact]
    public void NormalizeForCacheKey_PermutedKeyOrder_ProducesIdenticalCanonicalJson()
    {
        var body1 = Encoding.UTF8.GetBytes("{\"a\":1,\"b\":2}");
        var body2 = Encoding.UTF8.GetBytes("{\"b\":2,\"a\":1}");
        var r1 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body1, Array.Empty<string>());
        var r2 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body2, Array.Empty<string>());

        Encoding.UTF8.GetString(r1).Should().Be(Encoding.UTF8.GetString(r2));
        Encoding.UTF8.GetString(r1).Should().Be("{\"a\":1,\"b\":2}");
    }

    [Fact]
    public void NormalizeForCacheKey_NoMatchingFields_StillReSerializesForCanonicalBytes()
    {
        var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "tool_call_id" });

        ReferenceEquals(result, body).Should().BeFalse();
        Encoding.UTF8.GetString(result).Should().Contain("\"model\":\"gpt-4\"");
    }

    [Fact]
    public void NormalizeForCacheKey_TopLevelProperty_IsNormalized()
    {
        var body = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_abc123\",\"model\":\"gpt-4\"}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "tool_call_id" });
        var json = Encoding.UTF8.GetString(result);

        json.Should().Contain("\"tool_call_id\":\"__NORMALIZED__\"");
        json.Should().Contain("\"model\":\"gpt-4\"");
    }

    [Fact]
    public void NormalizeForCacheKey_DeeplyNestedProperty_IsFoundRecursively()
    {
        var body = Encoding.UTF8.GetBytes("{\"outer\":{\"inner\":{\"id\":\"guid-123\"}}}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "id" });
        var json = Encoding.UTF8.GetString(result);

        json.Should().Contain("\"id\":\"__NORMALIZED__\"");
    }

    [Fact]
    public void NormalizeForCacheKey_PropertiesInsideArrays_AreNormalized()
    {
        var body = Encoding.UTF8.GetBytes("{\"items\":[{\"id\":\"a\"},{\"id\":\"b\"}]}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "id" });
        var json = Encoding.UTF8.GetString(result);

        json.Should().NotContain("\"id\":\"a\"");
        json.Should().NotContain("\"id\":\"b\"");
        json.Should().Contain("\"id\":\"__NORMALIZED__\"");
    }

    [Fact]
    public void NormalizeForCacheKey_MultipleProperties_AllNormalized()
    {
        var body = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_1\",\"created\":1234567890,\"model\":\"gpt-4\"}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "tool_call_id", "created" });
        var json = Encoding.UTF8.GetString(result);

        json.Should().Contain("\"tool_call_id\":\"__NORMALIZED__\"");
        json.Should().Contain("\"created\":\"__NORMALIZED__\"");
        json.Should().Contain("\"model\":\"gpt-4\"");
    }

    [Fact]
    public void NormalizeForCacheKey_NonJsonBody_ReturnsUnchanged()
    {
        var body = Encoding.UTF8.GetBytes("grant_type=client_credentials&scope=openid");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "grant_type" });

        result.Should().BeSameAs(body);
    }

    [Fact]
    public void NormalizeForCacheKey_DifferentDynamicValues_ProduceIdenticalOutput()
    {
        var body1 = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_abc123\",\"model\":\"gpt-4\"}");
        var body2 = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_xyz789\",\"model\":\"gpt-4\"}");
        var fields = new[] { "tool_call_id" };

        var result1 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body1, fields);
        var result2 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body2, fields);

        Encoding.UTF8.GetString(result1).Should().Be(Encoding.UTF8.GetString(result2));
    }

    [Fact]
    public void NormalizeForCacheKey_DifferentDynamicValues_ProduceSameCacheKey()
    {
        var body1 = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_abc\",\"model\":\"gpt-4\",\"messages\":[]}");
        var body2 = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_xyz\",\"model\":\"gpt-4\",\"messages\":[]}");
        var fields = new List<string> { "tool_call_id" };

        var hash1 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body1, dynamicContentFields: fields);
        var hash2 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body2, dynamicContentFields: fields);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void NormalizeForCacheKey_CrLfAndLfInStringValues_ProduceIdenticalOutput()
    {
        // Simulates the real-world problem: StringBuilder.AppendLine() produces \r\n on Windows
        // and \n on Linux. Both should produce the same normalized output for cache keys.
        var windowsBody = Encoding.UTF8.GetBytes(
            "{\"messages\":[{\"role\":\"system\",\"content\":\"CURRENT CONTEXT:\\r\\nFeature: Login\\r\\nAs a user\"}]}");
        var linuxBody = Encoding.UTF8.GetBytes(
            "{\"messages\":[{\"role\":\"system\",\"content\":\"CURRENT CONTEXT:\\nFeature: Login\\nAs a user\"}]}");

        var r1 = CacheKeyBodyNormalizer.NormalizeForCacheKey(windowsBody, Array.Empty<string>());
        var r2 = CacheKeyBodyNormalizer.NormalizeForCacheKey(linuxBody, Array.Empty<string>());

        Encoding.UTF8.GetString(r1).Should().Be(Encoding.UTF8.GetString(r2));
    }

    [Fact]
    public void NormalizeForCacheKey_CrLfInStringValues_ProducesSameCacheKey()
    {
        var windowsBody = Encoding.UTF8.GetBytes(
            "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"line1\\r\\nline2\\r\\nline3\"}]}");
        var linuxBody = Encoding.UTF8.GetBytes(
            "{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"line1\\nline2\\nline3\"}]}");

        var hash1 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, windowsBody);
        var hash2 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, linuxBody);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void NormalizeForCacheKey_CrLfDeeplyNested_IsNormalized()
    {
        var body = Encoding.UTF8.GetBytes(
            "{\"outer\":{\"inner\":{\"text\":\"hello\\r\\nworld\"}}}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, Array.Empty<string>());
        var json = Encoding.UTF8.GetString(result);

        json.Should().NotContain("\\r\\n");
        json.Should().Contain("\\nworld");
    }

    [Fact]
    public void NormalizeForCacheKey_CrLfInArrayStrings_IsNormalized()
    {
        var body = Encoding.UTF8.GetBytes(
            "{\"items\":[\"line1\\r\\nline2\",\"line3\\r\\nline4\"]}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, Array.Empty<string>());
        var json = Encoding.UTF8.GetString(result);

        json.Should().NotContain("\\r\\n");
        json.Should().Contain("\\nline2");
        json.Should().Contain("\\nline4");
    }

    [Fact]
    public void NormalizeForCacheKey_StandaloneCr_IsNormalized()
    {
        // Standalone \r (old Mac line endings) should also be normalized to \n
        var body = Encoding.UTF8.GetBytes(
            "{\"text\":\"line1\\rline2\"}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, Array.Empty<string>());
        var json = Encoding.UTF8.GetString(result);

        json.Should().NotContain("\\r");
        json.Should().Contain("\\nline2");
    }

    [Fact]
    public void NormalizeForCacheKey_EmptyBody_ReturnsUnchanged()
    {
        var body = Array.Empty<byte>();
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, new[] { "id" });

        result.Should().BeSameAs(body);
    }

    [Fact]
    public void NormalizeForCacheKey_ProducesDeterministicOutput_NoPlatformDifferences()
    {
        // This test ensures the normalized output is deterministic and platform-independent.
        // It should produce identical byte sequences on Windows and Linux.
        var body = Encoding.UTF8.GetBytes("{\"tool_call_id\":\"call_123\",\"model\":\"gpt-4\",\"temperature\":0.7,\"messages\":[{\"role\":\"user\",\"content\":\"hello\"}]}");
        var fields = new[] { "tool_call_id" };

        // Run normalization twice to verify stability
        var result1 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, fields);
        var result2 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, fields);
        var json = Encoding.UTF8.GetString(result1);

        // Verify the output is compact (no indentation/whitespace that could differ by platform)
        json.Should().NotContain("\r\n");
        json.Should().NotContain("\n");
        json.Should().NotContain("  "); // No indentation spaces

        // Verify normalization occurred
        json.Should().Contain("\"tool_call_id\":\"__NORMALIZED__\"");

        // Verify non-normalized fields are preserved
        json.Should().Contain("\"model\":\"gpt-4\"");
        json.Should().Contain("\"content\":\"hello\"");

        // Verify successive calls produce identical byte sequences
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public void NormalizeForCacheKey_SpecialCharacters_HandledConsistentlyAcrossPlatforms()
    {
        // Exercises characters affected by JSON escaping differences: non-ASCII,
        // and HTML-sensitive characters (<, >, &). UnsafeRelaxedJsonEscaping should pass
        // these through verbatim rather than escaping to \uXXXX sequences.
        // Note: Emoji (surrogate pairs) are still escaped to \uXXXX by the encoder,
        // but the escaping is consistent across platforms which is what matters.
        var body = Encoding.UTF8.GetBytes("{\"id\":\"123\",\"content\":\"<div>Héllo & wörld</div> 🚀\"}");
        var fields = new[] { "id" };

        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, fields);
        var json = Encoding.UTF8.GetString(result);

        // Verify normalization occurred on the target field
        json.Should().Contain("\"id\":\"__NORMALIZED__\"");

        // Verify HTML-sensitive and non-ASCII characters are passed through verbatim
        json.Should().Contain("<div>");
        json.Should().Contain("</div>");
        json.Should().Contain("&");
        json.Should().Contain("Héllo");
        json.Should().Contain("wörld");

        // Verify no \uXXXX escaping of HTML-sensitive or non-ASCII characters
        json.Should().NotContain("\\u003C"); // <
        json.Should().NotContain("\\u003E"); // >
        json.Should().NotContain("\\u0026"); // &
        json.Should().NotContain("\\u00E9"); // é
        json.Should().NotContain("\\u00F6"); // ö

        // Emoji (surrogate pairs) are escaped but consistently across platforms
        json.Should().Contain("\\uD83D\\uDE80"); // 🚀 as surrogate pair

        // Verify successive calls produce identical output
        var result2 = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, fields);
        result.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public void NormalizeForCacheKey_ComplexNestedStructure_ProducesDeterministicCacheKey()
    {
        // Ensure that complex nested structures produce the same cache key regardless of platform
        var body1 = Encoding.UTF8.GetBytes("{\"messages\":[{\"role\":\"user\",\"content\":\"test\",\"tool_call_id\":\"call_a\"}],\"tools\":[{\"type\":\"function\",\"function\":{\"name\":\"search\",\"id\":\"id_123\"}}]}");
        var body2 = Encoding.UTF8.GetBytes("{\"messages\":[{\"role\":\"user\",\"content\":\"test\",\"tool_call_id\":\"call_b\"}],\"tools\":[{\"type\":\"function\",\"function\":{\"name\":\"search\",\"id\":\"id_456\"}}]}");
        var fields = new List<string> { "tool_call_id", "id" };

        var hash1 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body1, dynamicContentFields: fields);
        var hash2 = CacheKeyComputer.ComputeCacheKey("POST", "/v1/chat/completions", null, body2, dynamicContentFields: fields);

        // Both should produce identical cache keys after normalization
        hash1.Should().Be(hash2);
    }
}
