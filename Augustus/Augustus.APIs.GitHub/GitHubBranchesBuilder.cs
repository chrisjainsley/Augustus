namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub branches API endpoints.
/// </summary>
public sealed class GitHubBranchesBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubBranchesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/branches/{branch} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/branches/{branch}", "GET", "branch");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/branches endpoint (list branches).
    /// </summary>
    public GitHubResourceConfigurer List()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/branches", "GET", "branch_list");
    }
}
