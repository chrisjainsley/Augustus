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
}
