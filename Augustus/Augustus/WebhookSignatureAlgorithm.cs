namespace Augustus;

/// <summary>
/// Specifies the HMAC algorithm used to sign webhook payloads.
/// </summary>
public enum WebhookSignatureAlgorithm
{
    /// <summary>
    /// HMAC-SHA256 signing (default).
    /// </summary>
    HmacSha256 = 0,

    /// <summary>
    /// HMAC-SHA512 signing.
    /// </summary>
    HmacSha512 = 1,
}
