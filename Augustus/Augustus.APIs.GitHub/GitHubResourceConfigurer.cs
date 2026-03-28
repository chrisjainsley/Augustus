namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Configures response strategies for a GitHub API endpoint.
/// </summary>
public sealed class GitHubResourceConfigurer
{
    private readonly APISimulator apiSimulator;
    private readonly string pattern;
    private readonly string httpMethod;
    private readonly string resourceType;
    private readonly int successStatusCode;

    internal GitHubResourceConfigurer(
        APISimulator apiSimulator,
        string pattern,
        string httpMethod,
        string resourceType,
        int successStatusCode = 200)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
        this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this.httpMethod = httpMethod ?? throw new ArgumentNullException(nameof(httpMethod));
        this.resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        this.successStatusCode = successStatusCode;
    }

    /// <summary>
    /// Uses the built-in default realistic GitHub response for this endpoint.
    /// </summary>
    public void UseDefault()
    {
        var defaultResponse = GitHubDefaults.GetDefaultResponse(resourceType);
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithStatusCode(successStatusCode)
            .WithResponse(defaultResponse)
            .Add();
    }

    /// <summary>
    /// Uses a custom JSON response string.
    /// </summary>
    /// <param name="jsonResponse">The JSON response to return.</param>
    public void WithResponse(string jsonResponse)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithStatusCode(successStatusCode)
            .WithResponse(jsonResponse)
            .Add();
    }

    /// <summary>
    /// Uses a custom response object (will be serialized to JSON).
    /// </summary>
    /// <param name="responseObject">The object to serialize and return.</param>
    public void WithResponse(object responseObject)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithStatusCode(successStatusCode)
            .WithResponse(responseObject)
            .Add();
    }

    /// <summary>
    /// Uses a JSON file for the response.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    public void WithJsonFile(string filePath)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithStatusCode(successStatusCode)
            .WithJsonFile(filePath)
            .Add();
    }

    /// <summary>
    /// Uses a GitHub error response with the specified status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="errorJson">The GitHub error JSON (use <see cref="GitHubErrors"/> to generate).</param>
    public void WithError(int statusCode, string errorJson)
    {
        apiSimulator.ForRoute(pattern, httpMethod)
            .WithStatusCode(statusCode)
            .WithResponse(errorJson)
            .Add();
    }
}
