using Augustus;
using FluentAssertions;

namespace Augustus.Tests;

public class RequestKeyTests
{
    [Fact]
    public void RequestKey_SameScalarFields_AreEqual()
    {
        var instructions = new List<string> { "a", "b" };
        var k1 = new RequestKey("POST", "/v1/chat/completions", "?x=1", instructions, "{\"m\":1}");
        var k2 = new RequestKey("POST", "/v1/chat/completions", "?x=1", instructions, "{\"m\":1}");

        k2.Should().Be(k1);
        k2.GetHashCode().Should().Be(k1.GetHashCode());
    }

    [Fact]
    public void RequestKey_WithCopy_RewritesPathOnly()
    {
        var original = new RequestKey(
            "POST",
            "/openai/deployments/gpt-4-prod/chat/completions",
            null,
            Array.Empty<string>(),
            "{}");

        var rewritten = original with
        {
            Path = "/openai/deployments/__DEPLOYMENT__/chat/completions"
        };

        rewritten.Path.Should().Be("/openai/deployments/__DEPLOYMENT__/chat/completions");
        rewritten.Method.Should().Be(original.Method);
        rewritten.NormalizedBody.Should().Be(original.NormalizedBody);
        rewritten.Should().NotBe(original);
    }

    [Fact]
    public void CanonicalRequest_RoundTripsScalarFields()
    {
        var canonical = new CanonicalRequest("GET", "/health", "?probe=1", Array.Empty<string>(), "");

        canonical.Method.Should().Be("GET");
        canonical.Path.Should().Be("/health");
        canonical.QueryString.Should().Be("?probe=1");
        canonical.NormalizedBody.Should().BeEmpty();
    }

    [Fact]
    public void CacheMissDiagnostic_ExposesExpectedIdentity()
    {
        var canonical = new CanonicalRequest("POST", "/v1/chat/completions", null, Array.Empty<string>(), "{}");
        var diagnostic = new CacheMissDiagnostic(canonical, "ABCDEF", "/tmp/mocks");

        diagnostic.ExpectedCanonicalRequest.Should().Be(canonical);
        diagnostic.ComputedKey.Should().Be("ABCDEF");
        diagnostic.CachePath.Should().Be("/tmp/mocks");
    }
}
