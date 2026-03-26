namespace Augustus.APIs.GitHub;

using System.Text.Json;

/// <summary>
/// Generates standard GitHub API error JSON envelopes.
/// </summary>
public static class GitHubErrors
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public static string NotFound(string message = "Not Found", string? documentationUrl = null)
    {
        return GenerateErrorJson(message, documentationUrl: documentationUrl ?? "https://docs.github.com/rest");
    }

    public static string Unauthorized(string message = "Bad credentials", string? documentationUrl = null)
    {
        return GenerateErrorJson(message, documentationUrl: documentationUrl ?? "https://docs.github.com/rest");
    }

    public static string Forbidden(string message = "Resource not accessible by integration", string? documentationUrl = null)
    {
        return GenerateErrorJson(message, documentationUrl: documentationUrl ?? "https://docs.github.com/rest");
    }

    public static string RateLimitExceeded(string message = "API rate limit exceeded", string? documentationUrl = null)
    {
        return GenerateErrorJson(message, documentationUrl: documentationUrl ?? "https://docs.github.com/rest/overview/rate-limits-for-the-rest-api");
    }

    public static string ValidationFailed(string message = "Validation Failed", GitHubValidationError[]? errors = null, string? documentationUrl = null)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["documentation_url"] = documentationUrl ?? "https://docs.github.com/rest"
        };

        if (errors != null && errors.Length > 0)
        {
            envelope["errors"] = errors.Select(e =>
            {
                var errorObj = new Dictionary<string, string>();
                if (e.Resource != null) errorObj["resource"] = e.Resource;
                if (e.Field != null) errorObj["field"] = e.Field;
                if (e.Code != null) errorObj["code"] = e.Code;
                return errorObj;
            }).ToArray();
        }

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public static string UnprocessableEntity(string message = "Validation Failed", GitHubValidationError[]? errors = null, string? documentationUrl = null)
    {
        return ValidationFailed(message, errors, documentationUrl);
    }

    public static string ServerError(string message = "Internal Server Error", string? documentationUrl = null)
    {
        return GenerateErrorJson(message, documentationUrl: documentationUrl ?? "https://docs.github.com/rest");
    }

    private static string GenerateErrorJson(string message, string? documentationUrl = null)
    {
        var envelope = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["documentation_url"] = documentationUrl
        };

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }
}

/// <summary>
/// Represents a validation error in a GitHub API response.
/// </summary>
public sealed class GitHubValidationError
{
    public string? Resource { get; set; }
    public string? Field { get; set; }
    public string? Code { get; set; }

    public GitHubValidationError() { }

    public GitHubValidationError(string resource, string field, string code)
    {
        Resource = resource;
        Field = field;
        Code = code;
    }
}
