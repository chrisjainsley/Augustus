using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Augustus.AI;

namespace Augustus.Sample.StripeApi.Tests;

/// <summary>
/// Deterministic check that a generated Stripe response echoes the request: scalar fields sent in the
/// body (amount, currency, customer, ...) come back unchanged, and a retrieve returns the requested id.
/// </summary>
internal sealed partial class StripeRequestEchoValidator : IAIResponseValidator
{
    private static readonly string[] EchoedFields = { "amount", "currency", "customer", "email", "name", "description" };

    public ValueTask<AIResponseValidationResult> ValidateAsync(
        AIGenerationContext context,
        JsonElement response,
        CancellationToken cancellationToken)
    {
        if (response.ValueKind != JsonValueKind.Object)
            return new(AIResponseValidationResult.Valid);

        var sent = ParseBody(context.SanitizedCurlRequest);
        foreach (var field in EchoedFields)
        {
            if (!sent.TryGetValue(field, out var expected) || !response.TryGetProperty(field, out var actual))
                continue;
            if (!Matches(expected, actual))
                return new(AIResponseValidationResult.Invalid(
                    $"field \"{field}\" is {actual.GetRawText()} but the request sent \"{expected}\""));
        }

        var requestedId = RequestedResourceId(context);
        if (requestedId is not null
            && response.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String
            && id.GetString() != requestedId)
        {
            return new(AIResponseValidationResult.Invalid(
                $"field \"id\" is \"{id.GetString()}\" but the request retrieved \"{requestedId}\""));
        }

        return new(AIResponseValidationResult.Valid);
    }

    private static bool Matches(string expected, JsonElement actual) => actual.ValueKind switch
    {
        JsonValueKind.Number => decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            && actual.TryGetDecimal(out var value) && value == number,
        JsonValueKind.String => string.Equals(actual.GetString(), expected, StringComparison.OrdinalIgnoreCase),
        // Objects (an expanded customer) and nulls are not an echo mismatch.
        _ => true
    };

    /// <summary>GET /v1/{resource}/{id} retrieves one object whose id must match.</summary>
    private static string? RequestedResourceId(AIGenerationContext context)
    {
        if (!string.Equals(context.Method, "GET", StringComparison.OrdinalIgnoreCase))
            return null;
        var segments = context.Path.Trim('/').Split('/');
        return segments.Length == 3 && segments[0] == "v1" ? segments[2] : null;
    }

    private static Dictionary<string, string> ParseBody(string curl)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var match = BodyPattern().Match(curl);
        if (!match.Success)
            return fields;

        var body = match.Groups["body"].Value.Replace("'\''", "'");
        if (body.TrimStart().StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind is JsonValueKind.String)
                        fields[property.Name] = property.Value.GetString()!;
                    else if (property.Value.ValueKind is JsonValueKind.Number)
                        fields[property.Name] = property.Value.GetRawText();
                }
            }
            catch (JsonException)
            {
            }
            return fields;
        }

        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                fields[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1].Replace('+', ' '));
        }
        return fields;
    }

    [GeneratedRegex(@" -d '(?<body>(?:[^']|'\'')*)'")]
    private static partial Regex BodyPattern();
}
