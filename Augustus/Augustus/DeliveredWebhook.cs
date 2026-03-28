namespace Augustus;

/// <summary>
/// Represents a webhook that was delivered (or attempted) by the simulator.
/// Exposed via <see cref="APISimulator.DeliveredWebhooks"/> for test assertions.
/// </summary>
/// <param name="EventType">The webhook event type (e.g., "customer.subscription.created").</param>
/// <param name="Payload">The JSON payload that was sent.</param>
/// <param name="Timestamp">When the delivery was attempted.</param>
/// <param name="Success">Whether the delivery succeeded (HTTP 2xx from receiver).</param>
/// <param name="StatusCode">The HTTP status code returned by the receiver, or null if the request failed.</param>
/// <param name="ErrorMessage">The error message if delivery failed, or null on success.</param>
public sealed record DeliveredWebhook(
    string EventType,
    string Payload,
    DateTimeOffset Timestamp,
    bool Success,
    int? StatusCode,
    string? ErrorMessage);
