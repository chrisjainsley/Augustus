using Microsoft.AspNetCore.Http;
using System.Text;

namespace Augustus;

public static class HttpRequestExtensions
{
    public static async Task<string> ToCurlCommandAsync(this HttpRequest request)
    {
        StringBuilder curlCommand = new StringBuilder(256); // Pre-allocate with estimated capacity
        curlCommand.Append("curl -X ").Append(request.Method);

        // Append headers (escape double quotes in header values)
        foreach (var header in request.Headers)
        {
            var escapedValue = EscapeForDoubleQuotes(header.Value.ToString());
            curlCommand.Append($" -H \"{header.Key}: {escapedValue}\"");
        }

        // Append request body
        if (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
            request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
        {
            // Only attempt to rewind if the stream supports seeking
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }

            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            {
                string requestBody = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(requestBody))
                {
                    // Escape single quotes in the request body for shell safety
                    var escapedBody = EscapeForSingleQuotes(requestBody);
                    curlCommand.Append($" -d '{escapedBody}'");
                }
            }

            // Reset position for subsequent reads, if possible
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }

        // Append URL
        curlCommand.Append($" \"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}\"");

        return curlCommand.ToString();
    }

    /// <summary>
    /// Escapes a string for use inside double quotes in a shell command.
    /// </summary>
    private static string EscapeForDoubleQuotes(string value)
    {
        // Escape backslashes and double quotes
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    /// <summary>
    /// Escapes a string for use inside single quotes in a shell command.
    /// In single-quoted strings, the only character that needs escaping is the single quote itself.
    /// We do this by ending the single-quoted string, adding an escaped single quote, and starting a new single-quoted string.
    /// </summary>
    private static string EscapeForSingleQuotes(string value)
    {
        // Replace ' with '\'' (end quote, escaped quote, start quote)
        return value.Replace("'", "'\\''");
    }
}