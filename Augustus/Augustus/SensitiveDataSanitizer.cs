namespace Augustus;

using System.Text.RegularExpressions;

internal static class SensitiveDataSanitizer
{
    private const RegexOptions SanitizeOptions =
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    // Redact common credential headers in cURL-like text.
    private static readonly Regex AuthorizationHeaderRegex = new(
        "(authorization\\s*:\\s*)(?:bearer\\s+)?[^\\\"'\\r\\n]+",
        SanitizeOptions);

    private static readonly Regex ApiKeyHeaderRegex = new(
        "((?:x-)?api[-_]?key\\s*:\\s*)[^\\\"'\\r\\n]+",
        SanitizeOptions);

    // Redact sensitive key/value pairs in JSON-like payloads.
    private static readonly Regex JsonSensitiveKeyRegex = new(
        "(\\\"(?:api[-_]?key|access[-_]?token|refresh[-_]?token|token|secret|client[-_]?secret|password|passphrase|private[-_]?key)\\\"\\s*:\\s*\\\")[^\\\"]*(\\\")",
        SanitizeOptions);

    // Redact sensitive query/form parameters.
    private static readonly Regex QuerySensitiveParamRegex = new(
        "([?&](?:api[-_]?key|access[-_]?token|refresh[-_]?token|token|secret|client[-_]?secret|password|passphrase|private[-_]?key)=)[^&\\s\\\"']+",
        SanitizeOptions);

    // Redact generic key=value or key: value patterns in free-form text.
    private static readonly Regex FreeFormSensitiveKeyRegex = new(
        "(\\b(?:api[-_]?key|access[-_]?token|refresh[-_]?token|token|secret|client[-_]?secret|password|passphrase|private[-_]?key)\\b\\s*(?:=|:)\\s*)[^,\\s\\\"']+",
        SanitizeOptions);

    // Quick-check keywords that indicate a string might contain sensitive data.
    // If none are present, we can skip the expensive regex passes entirely.
    private static readonly string[] SensitiveKeywords = new[]
    {
        "authorization", "api", "key", "token", "secret", "password", "passphrase", "private"
    };

    public static string SanitizeSensitiveValues(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Fast path: skip regex passes if the input contains no sensitive keywords
        bool maybeSensitive = false;
        foreach (var keyword in SensitiveKeywords)
        {
            if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                maybeSensitive = true;
                break;
            }
        }

        if (!maybeSensitive)
        {
            return input;
        }

        var sanitized = AuthorizationHeaderRegex.Replace(input, "$1[REDACTED]");
        sanitized = ApiKeyHeaderRegex.Replace(sanitized, "$1[REDACTED]");
        sanitized = JsonSensitiveKeyRegex.Replace(sanitized, "$1[REDACTED]$2");
        sanitized = QuerySensitiveParamRegex.Replace(sanitized, "$1[REDACTED]");
        sanitized = FreeFormSensitiveKeyRegex.Replace(sanitized, "$1[REDACTED]");

        return sanitized;
    }
}
