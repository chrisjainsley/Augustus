namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub pull requests API endpoints.
/// </summary>
public sealed class GitHubPullRequestsBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubPullRequestsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/pulls/{pull_number} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/pulls/{pull_number}", "GET", "pull_request");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/pulls endpoint (list pull requests).
    /// </summary>
    public GitHubResourceConfigurer List()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/pulls", "GET", "pull_request_list");
    }

    /// <summary>
    /// Configures the POST /repos/{owner}/{repo}/pulls endpoint (create a pull request).
    /// </summary>
    public GitHubResourceConfigurer Create()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/pulls", "POST", "pull_request", successStatusCode: 201);
    }

    /// <summary>
    /// Configures the PATCH /repos/{owner}/{repo}/pulls/{pull_number} endpoint (update a pull request).
    /// </summary>
    public GitHubResourceConfigurer Update()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/pulls/{pull_number}", "PATCH", "pull_request");
    }
}
