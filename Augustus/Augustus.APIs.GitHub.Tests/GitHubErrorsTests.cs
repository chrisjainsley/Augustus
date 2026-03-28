using System.Text.Json;
using Augustus.APIs.GitHub;

namespace Augustus.APIs.GitHub.Tests;

public class GitHubErrorsTests
{
    [Fact]
    public void NotFound_ShouldHaveCorrectEnvelope()
    {
        var json = GitHubErrors.NotFound();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("message").GetString().Should().Be("Not Found");
        root.GetProperty("documentation_url").GetString().Should().Contain("docs.github.com");
    }

    [Fact]
    public void NotFound_WithCustomMessage_ShouldUseCustomMessage()
    {
        var json = GitHubErrors.NotFound("Repository not found");
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("message").GetString().Should().Be("Repository not found");
    }

    [Fact]
    public void Unauthorized_ShouldHaveCorrectDefaults()
    {
        var json = GitHubErrors.Unauthorized();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("message").GetString().Should().Be("Bad credentials");
        root.GetProperty("documentation_url").GetString().Should().Contain("docs.github.com");
    }

    [Fact]
    public void Forbidden_ShouldHaveCorrectDefaults()
    {
        var json = GitHubErrors.Forbidden();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("message").GetString().Should().Be("Resource not accessible by integration");
    }

    [Fact]
    public void RateLimitExceeded_ShouldHaveCorrectDefaults()
    {
        var json = GitHubErrors.RateLimitExceeded();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("message").GetString().Should().Be("API rate limit exceeded");
        root.GetProperty("documentation_url").GetString().Should().Contain("rate-limits");
    }

    [Fact]
    public void ValidationFailed_WithErrors_ShouldIncludeErrorsArray()
    {
        var json = GitHubErrors.ValidationFailed(errors: new[]
        {
            new GitHubValidationError("Issue", "title", "missing_field")
        });

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("message").GetString().Should().Be("Validation Failed");
        var errors = root.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
        errors[0].GetProperty("resource").GetString().Should().Be("Issue");
        errors[0].GetProperty("field").GetString().Should().Be("title");
        errors[0].GetProperty("code").GetString().Should().Be("missing_field");
    }

    [Fact]
    public void ValidationFailed_WithoutErrors_ShouldNotIncludeErrorsArray()
    {
        var json = GitHubErrors.ValidationFailed();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("errors", out _).Should().BeFalse();
    }

    [Fact]
    public void UnprocessableEntity_ShouldDelegateToValidationFailed()
    {
        var json = GitHubErrors.UnprocessableEntity("Custom validation message");
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("message").GetString().Should().Be("Custom validation message");
    }

    [Fact]
    public void ServerError_ShouldHaveCorrectDefaults()
    {
        var json = GitHubErrors.ServerError();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("message").GetString().Should().Be("Internal Server Error");
    }

    [Fact]
    public void AllErrors_ShouldProduceValidJson()
    {
        var errors = new[]
        {
            GitHubErrors.NotFound(),
            GitHubErrors.Unauthorized(),
            GitHubErrors.Forbidden(),
            GitHubErrors.RateLimitExceeded(),
            GitHubErrors.ValidationFailed(),
            GitHubErrors.UnprocessableEntity(),
            GitHubErrors.ServerError()
        };

        foreach (var errorJson in errors)
        {
            var parseAction = () => JsonDocument.Parse(errorJson);
            parseAction.Should().NotThrow();

            var doc = JsonDocument.Parse(errorJson);
            doc.RootElement.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        }
    }
}
