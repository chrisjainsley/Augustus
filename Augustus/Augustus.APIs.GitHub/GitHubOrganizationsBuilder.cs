namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub organizations API endpoints.
/// </summary>
public sealed class GitHubOrganizationsBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubOrganizationsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /orgs/{org} endpoint.
    /// </summary>
    public GitHubResourceConfigurer Get()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/orgs/{org}", "GET", "organization");
    }

    /// <summary>
    /// Configures the GET /user/orgs endpoint (list organizations for the authenticated user).
    /// </summary>
    public GitHubResourceConfigurer ListForAuthenticatedUser()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/user/orgs", "GET", "organization_list");
    }

    /// <summary>
    /// Configures the GET /users/{username}/orgs endpoint (list organizations for a user).
    /// </summary>
    public GitHubResourceConfigurer ListForUser()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/users/{username}/orgs", "GET", "organization_list");
    }
}
