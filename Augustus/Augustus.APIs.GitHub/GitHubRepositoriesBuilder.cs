namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub repository API endpoints.
/// </summary>
public sealed class GitHubRepositoriesBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubRepositoriesBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}", "GET", "repository");
    }

    /// <summary>
    /// Configures the GET /user/repos endpoint (list repositories for the authenticated user).
    /// </summary>
    public GitHubResourceConfigurer ListForAuthenticatedUser()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/user/repos", "GET", "repository_list");
    }

    /// <summary>
    /// Configures the GET /users/{username}/repos endpoint (list repositories for a user).
    /// </summary>
    public GitHubResourceConfigurer ListForUser()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/users/{username}/repos", "GET", "repository_list");
    }

    /// <summary>
    /// Configures the POST /user/repos endpoint (create a repository).
    /// </summary>
    public GitHubResourceConfigurer Create()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/user/repos", "POST", "repository", successStatusCode: 201);
    }

    /// <summary>
    /// Configures the PATCH /repos/{owner}/{repo} endpoint (update a repository).
    /// </summary>
    public GitHubResourceConfigurer Update()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}", "PATCH", "repository");
    }

    /// <summary>
    /// Configures the DELETE /repos/{owner}/{repo} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Delete()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}", "DELETE", "repository_deleted", successStatusCode: 204);
    }
}
