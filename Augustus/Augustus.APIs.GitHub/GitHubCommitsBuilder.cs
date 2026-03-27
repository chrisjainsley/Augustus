namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub commits API endpoints.
/// </summary>
public sealed class GitHubCommitsBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubCommitsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/commits/{ref} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/commits/{ref}", "GET", "commit");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/commits endpoint (list commits).
    /// </summary>
    public GitHubResourceConfigurer List()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/commits", "GET", "commit_list");
    }
}
