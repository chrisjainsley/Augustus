namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub releases API endpoints.
/// </summary>
public sealed class GitHubReleasesBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubReleasesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/releases/{release_id} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/releases/{release_id}", "GET", "release");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/releases endpoint (list releases).
    /// </summary>
    public GitHubResourceConfigurer List()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/releases", "GET", "release_list");
    }

    /// <summary>
    /// Configures the POST /repos/{owner}/{repo}/releases endpoint (create a release).
    /// </summary>
    public GitHubResourceConfigurer Create()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/releases", "POST", "release", successStatusCode: 201);
    }
}
