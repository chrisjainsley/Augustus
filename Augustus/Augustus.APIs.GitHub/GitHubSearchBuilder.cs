namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub search API endpoints.
/// </summary>
public sealed class GitHubSearchBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubSearchBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /search/repositories endpoint.
    /// </summary>
    public GitHubResourceConfigurer Repositories()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/search/repositories", "GET", "search_repositories");
    }

    /// <summary>
    /// Configures the GET /search/issues endpoint.
    /// </summary>
    public GitHubResourceConfigurer Issues()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/search/issues", "GET", "search_issues");
    }

    /// <summary>
    /// Configures the GET /search/code endpoint.
    /// </summary>
    public GitHubResourceConfigurer Code()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/search/code", "GET", "search_code");
    }
}
