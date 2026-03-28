namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub Git references API endpoints.
/// </summary>
public sealed class GitHubGitRefsBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubGitRefsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/git/ref/{*} endpoint.
    /// Uses a wildcard to support slash-delimited refs (e.g., <c>heads/main</c>, <c>tags/v1.0</c>).
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/git/ref/{*}", "GET", "git_ref");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/git/matching-refs/{*} endpoint (list matching references).
    /// Uses a wildcard to support slash-delimited ref prefixes (e.g., <c>heads/</c>, <c>tags/v1</c>).
    /// </summary>
    public GitHubResourceConfigurer List()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/git/matching-refs/{*}", "GET", "git_ref_list");
    }

    /// <summary>
    /// Configures the POST /repos/{owner}/{repo}/git/refs endpoint (create a reference).
    /// </summary>
    public GitHubResourceConfigurer Create()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/git/refs", "POST", "git_ref", successStatusCode: 201);
    }
}
