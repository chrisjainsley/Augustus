namespace Augustus;

using System.Net.Http;

/// <summary>
/// Represents an HTTP request that was received and recorded by the API simulator.
/// </summary>
public sealed class RecordedRequest
{
    /// <summary>
    /// Gets the HTTP method of the request (e.g., GET, POST, PUT).
    /// </summary>
    public HttpMethod Method { get; init; } = HttpMethod.Get;

    /// <summary>
    /// Gets the request path (e.g., "/v1/customers").
    /// </summary>
    public string Path { get; init; } = "/";

    /// <summary>
    /// Gets the request headers as a read-only dictionary.
    /// Multiple header values are joined with ", ".
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets the request body as a string, or null if no body was present.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the request was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
