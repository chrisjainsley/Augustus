namespace Augustus;

/// <summary>
/// Holds the configuration for a single webhook trigger: which requests match and what event to fire.
/// </summary>
internal sealed class WebhookTrigger
{
    private readonly RouteConfiguration routeMatcher;

    public string Pattern { get; }
    public string HttpMethod { get; }
    public string EventType { get; }
    public string? StaticPayload { get; init; }
    public Func<string>? PayloadFactory { get; init; }
    public TimeSpan Delay { get; init; } = TimeSpan.Zero;

    public WebhookTrigger(string pattern, string httpMethod, string eventType)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        HttpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        routeMatcher = new RouteConfiguration(pattern, httpMethod);
    }

    public bool Matches(string path, string method) => routeMatcher.Matches(path, method);
}
