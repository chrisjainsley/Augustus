using Microsoft.AspNetCore.Http;
using System.Text;

namespace Augustus;

public static class HttpRequestExtensions
{
    internal static readonly IReadOnlySet<string> DefaultAISkipHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Infrastructure headers — low signal, high token waste
        "Host", "Connection", "Keep-Alive", "Transfer-Encoding",
        "TE", "Trailer", "Upgrade", "Content-Length", "Accept-Encoding",
        "Proxy-Authorization", "Proxy-Authenticate",
        // Credential-bearing headers — must never be forwarded to the AI model
        "Authorization", "Cookie", "Set-Cookie", "x-api-key"
    };

    public static async Task<byte[]> ReadBodyBytesAsync(this HttpRequest request, CancellationToken cancellationToken = default)
    {
        request.EnableBuffering();
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        request.Body.Position = 0;
        return ms.ToArray();
    }

    public static Task<string> ToCurlCommandAsync(this HttpRequest request)
        => ToCurlCommandAsync(request, skipHeaders: null);

    public static async Task<string> ToCurlCommandAsync(this HttpRequest request, IReadOnlySet<string>? skipHeaders)
    {
        StringBuilder curlCommand = new StringBuilder(256); // Pre-allocate with estimated capacity
        curlCommand.Append("curl -X ").Append(request.Method);

        // Append headers (escape double quotes in header values)
        foreach (var header in request.Headers)
        {
            if (skipHeaders != null && skipHeaders.Contains(header.Key))
                continue;

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
                string requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
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