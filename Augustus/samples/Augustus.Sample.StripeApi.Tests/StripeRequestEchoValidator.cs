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

    /// <summary>Words that mark an instruction as asking for an error response.</summary>
    private static readonly string[] ErrorWords = { "error", "decline", "failed", "failure", "invalid" };

    /// <summary>Words that turn an error mention into a prohibition ("never return an error").</summary>
    private static readonly string[] NegationWords = { "never", "do not", "don't", "must not", "avoid" };

    public ValueTask<AIResponseValidationResult> ValidateAsync(
        AIGenerationContext context,
        JsonElement response,
        CancellationToken cancellationToken)
    {
        // Every Stripe response, success or error, is a JSON object.
        if (response.ValueKind != JsonValueKind.Object)
            return new(AIResponseValidationResult.Invalid(
                $"the response is a JSON {response.ValueKind.ToString().ToLowerInvariant()}, not a Stripe object"));

        // A generated body is always served with HTTP 200, so a Stripe error object is a broken success response
        // unless the instructions for this request ask for an error. A requested error echoes nothing.
        if (response.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
        {
            if (context.Instructions.Any(AsksForError))
                return new(AIResponseValidationResult.Valid);

            var message = error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            return new(AIResponseValidationResult.Invalid(
                $"field \"error\" is a Stripe error object{(message is null ? string.Empty : $" (\"{message}\")")} but the request expects a successful response"));
        }

        var sent = ParseBody(context.SanitizedCurlRequest);
        foreach (var field in EchoedFields)
        {
            if (!sent.TryGetValue(field, out var expected))
                continue;
            if (!response.TryGetProperty(field, out var actual))
                return new(AIResponseValidationResult.Invalid(
                    $"field \"{field}\" is missing but the request sent \"{expected}\""));
            if (!Matches(expected, actual))
                return new(AIResponseValidationResult.Invalid(
                    $"field \"{field}\" is {actual.GetRawText()} but the request sent \"{expected}\""));
        }

        var requestedId = RequestedResourceId(context);
        if (requestedId is not null
            && (!response.TryGetProperty("id", out var id)
                || id.ValueKind != JsonValueKind.String
                || id.GetString() != requestedId))
        {
            var actualId = response.TryGetProperty("id", out var found) ? found.GetRawText() : "missing";
            return new(AIResponseValidationResult.Invalid(
                $"field \"id\" is {actualId} but the request retrieved \"{requestedId}\""));
        }

        return new(AIResponseValidationResult.Valid);
    }

    private static bool AsksForError(string instruction)
        => ErrorWords.Any(word => instruction.Contains(word, StringComparison.OrdinalIgnoreCase))
            && !NegationWords.Any(word => instruction.Contains(word, StringComparison.OrdinalIgnoreCase));

    private static bool Matches(string expected, JsonElement actual) => actual.ValueKind switch
    {
        JsonValueKind.Number => decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            && actual.TryGetDecimal(out var value) && value == number,
        JsonValueKind.String => string.Equals(actual.GetString(), expected, StringComparison.OrdinalIgnoreCase),
        // An expanded object (a customer) still echoes the sent id; null or any other kind drops the sent value.
        JsonValueKind.Object => true,
        _ => false
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
                // A body that is not JSON sent no fields, so there is nothing for the response to echo.
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
