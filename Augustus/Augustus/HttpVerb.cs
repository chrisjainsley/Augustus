namespace Augustus;

/// <summary>
/// HTTP methods supported for route matching.
/// </summary>
public enum HttpVerb
{
    /// <summary>Matches any HTTP method.</summary>
    Any,

    /// <summary>HTTP GET.</summary>
    Get,

    /// <summary>HTTP POST.</summary>
    Post,

    /// <summary>HTTP PUT.</summary>
    Put,

    /// <summary>HTTP DELETE.</summary>
    Delete,

    /// <summary>HTTP PATCH.</summary>
    Patch
}

/// <summary>
/// Extension methods for <see cref="HttpVerb"/>.
/// </summary>
public static class HttpVerbExtensions
{
    /// <summary>
    /// Converts the <see cref="HttpVerb"/> to its uppercase string representation used internally for route matching.
    /// </summary>
    public static string ToMethodString(this HttpVerb verb) => verb switch
    {
        HttpVerb.Get => "GET",
        HttpVerb.Post => "POST",
        HttpVerb.Put => "PUT",
        HttpVerb.Delete => "DELETE",
        HttpVerb.Patch => "PATCH",
        HttpVerb.Any => "*",
        _ => "*"
    };
}
