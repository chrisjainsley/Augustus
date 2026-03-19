namespace Augustus.APIs.Stripe;

using System.Text.Json;

/// <summary>
/// Generates standard Stripe error JSON envelopes.
/// </summary>
public static class StripeErrors
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public static string InvalidRequestError(string message, string? param = null)
    {
        return GenerateErrorJson("invalid_request_error", message, param: param);
    }

    public static string CardError(string message, string? code = null, string? declineCode = null)
    {
        return GenerateErrorJson("card_error", message, code: code, declineCode: declineCode);
    }

    public static string AuthenticationError(string message = "Invalid API Key provided")
    {
        return GenerateErrorJson("authentication_error", message);
    }

    public static string RateLimitError(string message = "Too many requests")
    {
        return GenerateErrorJson("rate_limit_error", message);
    }

    public static string ApiError(string message = "An internal error occurred")
    {
        return GenerateErrorJson("api_error", message);
    }

    public static string IdempotencyError(string message)
    {
        return GenerateErrorJson("idempotency_error", message);
    }

    private static string GenerateErrorJson(string type, string message, string? code = null, string? param = null, string? declineCode = null)
    {
        var errorObj = new Dictionary<string, object?>
        {
            ["type"] = type,
            ["message"] = message
        };

        if (code != null)
            errorObj["code"] = code;

        if (param != null)
            errorObj["param"] = param;

        if (declineCode != null)
            errorObj["decline_code"] = declineCode;

        var envelope = new Dictionary<string, object>
        {
            ["error"] = errorObj
        };

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }
}
