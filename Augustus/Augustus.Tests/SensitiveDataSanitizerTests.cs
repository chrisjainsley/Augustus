using FluentAssertions;

namespace Augustus.Tests;

public class SensitiveDataSanitizerTests
{
    [Theory]
    [InlineData("This is a plain string with no secrets")]
    [InlineData("Hello world")]
    [InlineData("GET /v1/customers/123 HTTP/1.1")]
    [InlineData("")]
    public void SanitizeSensitiveValues_ShouldReturnUnchanged_WhenNoSensitiveKeywordsPresent(string input)
    {
        var result = SensitiveDataSanitizer.SanitizeSensitiveValues(input);

        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeSensitiveValues_ShouldRedact_WhenAuthorizationHeaderPresent()
    {
        var input = "curl -H \"Authorization: Bearer sk-abc123\" https://api.example.com";

        var result = SensitiveDataSanitizer.SanitizeSensitiveValues(input);

        result.Should().NotContain("sk-abc123");
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void SanitizeSensitiveValues_ShouldRedact_WhenApiKeyHeaderPresent()
    {
        var input = "curl -H \"x-api-key: my-secret-key\" https://example.com";

        var result = SensitiveDataSanitizer.SanitizeSensitiveValues(input);

        result.Should().NotContain("my-secret-key");
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void SanitizeSensitiveValues_ShouldRedact_WhenJsonSensitiveKeysPresent()
    {
        var input = "{\"password\":\"p@ssw0rd\",\"name\":\"John\"}";

        var result = SensitiveDataSanitizer.SanitizeSensitiveValues(input);

        result.Should().NotContain("p@ssw0rd");
        result.Should().Contain("John");
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void SanitizeSensitiveValues_ShouldRedact_WhenQueryParamsContainSecrets()
    {
        var input = "https://example.com/v1/items?api_key=qwerty&page=1";

        var result = SensitiveDataSanitizer.SanitizeSensitiveValues(input);

        result.Should().NotContain("qwerty");
        result.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void SanitizeSensitiveValues_ShouldHandleNullAndWhitespace()
    {
        SensitiveDataSanitizer.SanitizeSensitiveValues(null!).Should().BeNull();
        SensitiveDataSanitizer.SanitizeSensitiveValues("").Should().Be("");
        SensitiveDataSanitizer.SanitizeSensitiveValues("   ").Should().Be("   ");
    }

    [Fact]
    public void SanitizeSensitiveValues_ShouldRedactMultipleSensitiveValues()
    {
        var input = "curl -H \"Authorization: Bearer secret123\" -H \"x-api-key: key456\" -d '{\"token\":\"tok789\"}' \"https://example.com?password=pass000\"";

        var result = SensitiveDataSanitizer.SanitizeSensitiveValues(input);

        result.Should().NotContain("secret123");
        result.Should().NotContain("key456");
        result.Should().NotContain("tok789");
        result.Should().NotContain("pass000");
    }
}
