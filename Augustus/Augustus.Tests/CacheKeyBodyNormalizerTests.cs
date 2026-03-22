using System.Text;
using Augustus;
using FluentAssertions;

namespace Augustus.Tests;

public class CacheKeyBodyNormalizerTests
{
    [Fact]
    public void NormalizeForCacheKey_NoProperties_ReturnsBodyUnchanged()
    {
        var body = Encoding.UTF8.GetBytes("{\"id\":\"abc\",\"value\":42}");
        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, Array.Empty<string>());

        result.Should().BeSameAs(body);
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
        // Exercises characters affected by JSON escaping differences: non-ASCII, emoji,
        // and HTML-sensitive characters (<, >, &). UnsafeRelaxedJsonEscaping should pass
        // these through verbatim rather than escaping to \uXXXX sequences.
        var body = Encoding.UTF8.GetBytes("{\"id\":\"123\",\"content\":\"<div>Héllo & wörld</div> 🚀\"}");
        var fields = new[] { "id" };

        var result = CacheKeyBodyNormalizer.NormalizeForCacheKey(body, fields);
        var json = Encoding.UTF8.GetString(result);

        // Verify normalization occurred on the target field
        json.Should().Contain("\"id\":\"__NORMALIZED__\"");

        // Verify special characters are passed through verbatim (not escaped to \uXXXX)
        json.Should().Contain("<div>");
        json.Should().Contain("</div>");
        json.Should().Contain("&");
        json.Should().Contain("Héllo");
        json.Should().Contain("wörld");
        json.Should().Contain("\U0001f680"); // rocket emoji

        // Verify no \uXXXX escaping of these characters
        json.Should().NotContain("\\u003C"); // <
        json.Should().NotContain("\\u003E"); // >
        json.Should().NotContain("\\u0026"); // &

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
