using System.Text;
using Augustus;
using FluentAssertions;

namespace Augustus.Tests;

public class CacheKeyKnobsTests
{
    private static byte[] B(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void IgnoredQueryParameters_DoNotChurnKey()
    {
        var rules = new CacheKeyRules { IgnoredQueryParameters = new[] { "_t", "nonce" } };

        var a = RequestKeyFactory.Create("GET", "/v1/x", "?id=1&_t=111&nonce=aaa", Array.Empty<byte>(), null, rules);
        var b = RequestKeyFactory.Create("GET", "/v1/x", "?id=1&_t=999&nonce=zzz", Array.Empty<byte>(), null, rules);

        b.Hash.Should().Be(a.Hash);
    }

    [Fact]
    public void IgnoredQueryParameters_RetainedParameterStillMatters()
    {
        var rules = new CacheKeyRules { IgnoredQueryParameters = new[] { "_t" } };

        var a = RequestKeyFactory.Create("GET", "/v1/x", "?id=1&_t=1", Array.Empty<byte>(), null, rules);
        var b = RequestKeyFactory.Create("GET", "/v1/x", "?id=2&_t=1", Array.Empty<byte>(), null, rules);

        a.Hash.Should().NotBe(b.Hash);
    }

    [Fact]
    public void StripNullBodyProperties_NullVsOmitted_ProduceSameKey()
    {
        var rules = new CacheKeyRules { StripNullBodyProperties = true };

        var withNull = RequestKeyFactory.Create(
            "POST", "/v1/chat", null, B("{\"model\":\"gpt-4\",\"user\":null}"), null, rules);
        var omitted = RequestKeyFactory.Create(
            "POST", "/v1/chat", null, B("{\"model\":\"gpt-4\"}"), null, rules);

        withNull.Hash.Should().Be(omitted.Hash);
    }

    [Fact]
    public void StripNullBodyProperties_Disabled_NullVsOmitted_Differ()
    {
        var withNull = RequestKeyFactory.Create(
            "POST", "/v1/chat", null, B("{\"model\":\"gpt-4\",\"user\":null}"), null, null);
        var omitted = RequestKeyFactory.Create(
            "POST", "/v1/chat", null, B("{\"model\":\"gpt-4\"}"), null, null);

        withNull.Hash.Should().NotBe(omitted.Hash);
    }

    [Fact]
    public void HashMessagesContentOnly_CosmeticNonContentEdits_DoNotChurnKey()
    {
        var rules = new CacheKeyRules { HashMessagesContentOnly = true };

        var a = RequestKeyFactory.Create("POST", "/v1/chat", null,
            B("{\"model\":\"gpt-4\",\"temperature\":0.2,\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}"),
            null, rules);
        var b = RequestKeyFactory.Create("POST", "/v1/chat", null,
            B("{\"model\":\"gpt-4-turbo\",\"temperature\":0.9,\"messages\":[{\"role\":\"system\",\"content\":\"hi\"}]}"),
            null, rules);

        b.Hash.Should().Be(a.Hash);
    }

    [Fact]
    public void HashMessagesContentOnly_ContentEditStillChurnsKey()
    {
        var rules = new CacheKeyRules { HashMessagesContentOnly = true };

        var a = RequestKeyFactory.Create("POST", "/v1/chat", null,
            B("{\"messages\":[{\"role\":\"user\",\"content\":\"hello\"}]}"), null, rules);
        var b = RequestKeyFactory.Create("POST", "/v1/chat", null,
            B("{\"messages\":[{\"role\":\"user\",\"content\":\"goodbye\"}]}"), null, rules);

        a.Hash.Should().NotBe(b.Hash);
    }

    [Fact]
    public void Options_ExposeKnobsAndBuildRules()
    {
        var options = new APISimulatorOptions();
        options.IgnoredHeaders.Add("x-request-id");
        options.IgnoredQueryParameters.Add("_cb");
        options.StripNullBodyProperties = true;
        options.HashMessagesContentOnly = true;

        options.IgnoredHeaders.Should().Contain("x-request-id");
        options.IgnoredQueryParameters.Should().Contain("_cb");
        options.StripNullBodyProperties.Should().BeTrue();
        options.HashMessagesContentOnly.Should().BeTrue();
    }
}
