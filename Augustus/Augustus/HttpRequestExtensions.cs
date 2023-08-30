using Microsoft.AspNetCore.Http;
using System.Text;

namespace Augustus;

public static class HttpRequestExtensions
{
    public static async Task<string> ToCurlCommandAsync(this HttpRequest request)
    {
        StringBuilder curlCommand = new StringBuilder();
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
            request.Body.Position = 0;
            using (StreamReader reader = new StreamReader(request.Body))
            {
                string requestBody = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(requestBody))
                {
                    curlCommand.Append($" -d '{requestBody}'");
                }
            }
            request.Body.Position = 0;
        }

        // Append URL
        curlCommand.Append($" \"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}\"");

        return curlCommand.ToString();
    }
}