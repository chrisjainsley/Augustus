namespace Augustus;

using System.Text.RegularExpressions;

/// <summary>
/// Represents a route configuration for a mock server endpoint.
/// </summary>
public class RouteConfiguration
{
    /// <summary>
    /// Gets the URL pattern to match against incoming requests.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the HTTP method to match.
    /// </summary>
    public string HttpMethod { get; }

    /// <summary>
    /// Gets or sets the response strategy for this route.
    /// </summary>
    public IResponseStrategy? ResponseStrategy { get; set; }

    private readonly Regex compiledPattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteConfiguration"/> class.
    /// </summary>
    /// <param name="pattern">The URL pattern to match. Supports {id} for path segments and {*} for wildcards.</param>
    /// <param name="httpMethod">The HTTP method to match, or "*" for all methods. Default is "*".</param>
    public RouteConfiguration(string pattern, string httpMethod = "*")
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        HttpMethod = httpMethod?.ToUpperInvariant() ?? "*";
        compiledPattern = CompilePattern(pattern);
    }

    private static Regex CompilePattern(string pattern)
    {
        try
        {
            // Convert simple patterns like "/api/customers/{id}" to regex
            var regexPattern = pattern
                .Replace("{id}", @"[^/]+")
                .Replace("{*}", ".*")
                .Replace("/", @"\/");

            return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch
        {
            // If pattern compilation fails, treat as literal match
            return new Regex($"^{Regex.Escape(pattern)}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Determines whether this route matches the specified request path and HTTP method.
    /// </summary>
    /// <param name="path">The request path to test.</param>
    /// <param name="method">The HTTP method to test.</param>
    /// <returns>True if the path and method match this route; otherwise, false.</returns>
    public bool Matches(string path, string method)
    {
        if (HttpMethod != "*" && !HttpMethod.Equals(method, StringComparison.OrdinalIgnoreCase))
            return false;

        return compiledPattern.IsMatch(path);
    }
}
