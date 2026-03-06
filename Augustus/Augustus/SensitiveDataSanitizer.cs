namespace Augustus;

using System.Text.RegularExpressions;

internal static class SensitiveDataSanitizer
{
    public static string SanitizeSensitiveValues(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var sanitized = input;

        // Redact common credential headers in cURL-like text.
        sanitized = Regex.Replace(
            sanitized,
            @"(?i)(authorization\s*:\s*)(?:bearer\s+)?[^\"'\r\n]+",
            "$1[REDACTED]");

        sanitized = Regex.Replace(
            sanitized,
            @"(?i)((?:x-)?api[-_]?key\s*:\s*)[^\"'\r\n]+",
            "$1[REDACTED]");

        // Redact sensitive key/value pairs in JSON-like payloads.
        sanitized = Regex.Replace(
            sanitized,
            @"(?i)(\"(?:api[-_]?key|access[-_]?token|refresh[-_]?token|token|secret|client[-_]?secret|password|passphrase|private[-_]?key)\"\s*:\s*\")[^\"]*(\")",
            "$1[REDACTED]$2");

        // Redact sensitive query/form parameters.
        sanitized = Regex.Replace(
            sanitized,
            @"(?i)([?&](?:api[-_]?key|access[-_]?token|refresh[-_]?token|token|secret|client[-_]?secret|password|passphrase|private[-_]?key)=)[^&\s\"']+",
            "$1[REDACTED]");

        // Redact generic key=value or key: value patterns in free-form text.
        sanitized = Regex.Replace(
            sanitized,
            @"(?i)(\b(?:api[-_]?key|access[-_]?token|refresh[-_]?token|token|secret|client[-_]?secret|password|passphrase|private[-_]?key)\b\s*(?:=|:)\s*)[^,\s\"']+",
            "$1[REDACTED]");

        return sanitized;
    }
}
