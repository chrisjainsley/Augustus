namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub issues API endpoints.
/// </summary>
public sealed class GitHubIssuesBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubIssuesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/issues/{issue_number} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/issues/{issue_number}", "GET", "issue");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/issues endpoint (list issues).
    /// </summary>
    public GitHubResourceConfigurer List()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/issues", "GET", "issue_list");
    }

    /// <summary>
    /// Configures the POST /repos/{owner}/{repo}/issues endpoint (create an issue).
    /// </summary>
    public GitHubResourceConfigurer Create()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/issues", "POST", "issue", successStatusCode: 201);
    }

    /// <summary>
    /// Configures the PATCH /repos/{owner}/{repo}/issues/{issue_number} endpoint (update an issue).
    /// </summary>
    public GitHubResourceConfigurer Update()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/issues/{issue_number}", "PATCH", "issue");
    }
}
