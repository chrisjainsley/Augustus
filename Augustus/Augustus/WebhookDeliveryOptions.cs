namespace Augustus;

/// <summary>
/// Configuration options for webhook delivery, including the target URL and optional HMAC signing.
/// </summary>
public sealed class WebhookDeliveryOptions
{
    /// <summary>
    /// Gets the URL where webhook events will be POSTed.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets or sets the secret used to sign webhook payloads with HMAC.
    /// When set, each webhook delivery will include a signature header.
    /// </summary>
    public string? SigningSecret { get; set; }

    /// <summary>
    /// Gets or sets the HTTP header name used to transmit the HMAC signature.
    /// Default is "X-Signature".
    /// </summary>
    public string SignatureHeader { get; set; } = "X-Signature";

    /// <summary>
    /// Gets or sets the HMAC algorithm used for webhook signing.
    /// Default is <see cref="WebhookSignatureAlgorithm.HmacSha256"/>.
    /// </summary>
    public WebhookSignatureAlgorithm SignatureAlgorithm { get; set; } = WebhookSignatureAlgorithm.HmacSha256;

    /// <summary>
    /// Gets or sets an optional custom signer that takes full control of signature computation.
    /// When null, a standard HMAC hex digest is used as the header value.
    /// </summary>
    /// <remarks>
    /// Use this for provider-specific signing protocols. For example, Stripe computes
    /// HMAC over "{timestamp}.{payload}" and formats the header as "t={timestamp},v1={hmac}".
    /// </remarks>
    public WebhookSignerDelegate? WebhookSigner { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDeliveryOptions"/> class.
    /// </summary>
    /// <param name="url">The URL where webhook events will be POSTed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="url"/> is null.</exception>
    public WebhookDeliveryOptions(string url)
    {
        if (url is null)
            throw new ArgumentNullException(nameof(url));
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != "http" && parsed.Scheme != "https"))
            throw new ArgumentException($"Webhook URL must be an absolute HTTP or HTTPS URI, but got: '{url}'", nameof(url));
        Url = url;
    }
}

/// <summary>
/// Delegate for custom webhook signature computation.
/// </summary>
/// <param name="payload">The raw JSON payload being sent.</param>
/// <param name="signingSecret">The signing secret configured via <see cref="WebhookDeliveryOptions.SigningSecret"/>.</param>
/// <returns>The complete signature header value.</returns>
public delegate string WebhookSignerDelegate(string payload, string signingSecret);
