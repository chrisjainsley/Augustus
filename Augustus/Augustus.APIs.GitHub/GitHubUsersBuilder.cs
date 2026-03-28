namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub users API endpoints.
/// </summary>
public sealed class GitHubUsersBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubUsersBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /users/{username} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/users/{username}", "GET", "user");
    }

    /// <summary>
    /// Configures the GET /user endpoint (get the authenticated user).
    /// </summary>
    public GitHubResourceConfigurer GetAuthenticated()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/user", "GET", "user_authenticated");
    }
}
