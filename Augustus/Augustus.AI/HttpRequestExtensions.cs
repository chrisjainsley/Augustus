using Microsoft.AspNetCore.Http;
using System.Text;

namespace Augustus.AI;

internal static class HttpRequestExtensions
{
    public static async Task<string> ToCurlCommandAsync(this HttpRequest request)
    {
        StringBuilder curlCommand = new StringBuilder(256);
        curlCommand.Append("curl -X ").Append(request.Method);

        // Append headers
        foreach (var header in request.Headers)
        {
            curlCommand.Append($" -H \"{header.Key}: {header.Value}\"");
        }

        // Append request body
        if (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
            request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }

            using (StreamReader reader = new StreamReader(request.Body, leaveOpen: true))
            {
                string requestBody = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(requestBody))
                {
                    curlCommand.Append($" -d '{requestBody}'");
                }
            }

            if (request.Body.CanSeek)
            {
                request.Body.Position = 0;
            }
        }

        // Append URL
        curlCommand.Append($" \"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}\"");

        return curlCommand.ToString();
    }
}
