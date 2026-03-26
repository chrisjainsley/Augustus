namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Extension methods for adding GitHub-specific mocking capabilities to Augustus.
/// </summary>
public static class GitHubExtensions
{
    /// <summary>
    /// Creates a GitHub mock server with pre-configured GitHub API structure.
    /// </summary>
    /// <param name="testClass">The test class instance (typically 'this').</param>
    /// <param name="configureOptions">Optional action to configure server options.</param>
    /// <returns>A new <see cref="GitHubMock"/> instance.</returns>
    public static GitHubMock CreateGitHubMock(this object testClass, Action<APISimulatorOptions>? configureOptions = null)
    {
        var options = new APISimulatorOptions();
        configureOptions?.Invoke(options);
        var apiSimulator = new APISimulator(options);
        return new GitHubMock(apiSimulator);
    }

    /// <summary>
    /// Accesses GitHub-specific route configuration for an existing mock server.
    /// </summary>
    /// <param name="apiSimulator">The mock server instance.</param>
    /// <returns>A <see cref="GitHubRouteBuilder"/> for configuring GitHub routes.</returns>
    public static GitHubRouteBuilder GitHub(this APISimulator apiSimulator)
    {
        return new GitHubRouteBuilder(apiSimulator);
    }

    /// <summary>
    /// Accesses GitHub-specific route configuration for a GitHubMock instance.
    /// </summary>
    /// <param name="gitHubMock">The GitHub mock instance.</param>
    /// <returns>A <see cref="GitHubRouteBuilder"/> for configuring GitHub routes.</returns>
    public static GitHubRouteBuilder GitHub(this GitHubMock gitHubMock)
    {
        return new GitHubRouteBuilder(gitHubMock.APISimulator);
    }
}

/// <summary>
/// Wrapper around APISimulator that provides GitHub-specific fluent API at creation time.
/// </summary>
public sealed class GitHubMock
{
    private readonly APISimulator apiSimulator;

    internal GitHubMock(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Gets the underlying APISimulator instance.
    /// </summary>
    public APISimulator APISimulator => apiSimulator;

    /// <summary>
    /// Configures GitHub repository endpoints.
    /// </summary>
    public GitHubRepositoriesBuilder Repositories() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub issue endpoints.
    /// </summary>
    public GitHubIssuesBuilder Issues() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub pull request endpoints.
    /// </summary>
    public GitHubPullRequestsBuilder PullRequests() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub user endpoints.
    /// </summary>
    public GitHubUsersBuilder Users() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub organization endpoints.
    /// </summary>
    public GitHubOrganizationsBuilder Organizations() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub commit endpoints.
    /// </summary>
    public GitHubCommitsBuilder Commits() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub branch endpoints.
    /// </summary>
    public GitHubBranchesBuilder Branches() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub release endpoints.
    /// </summary>
    public GitHubReleasesBuilder Releases() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub Actions endpoints.
    /// </summary>
    public GitHubActionsBuilder Actions() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub Git references endpoints.
    /// </summary>
    public GitHubGitRefsBuilder GitRefs() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub search endpoints.
    /// </summary>
    public GitHubSearchBuilder Search() => new(apiSimulator);

    /// <summary>
    /// Registers all endpoints with default responses in one call.
    /// </summary>
    public GitHubMock UseAllDefaults()
    {
        // Repositories
        Repositories().Get().UseDefault();
        Repositories().ListForAuthenticatedUser().UseDefault();
        Repositories().ListForUser().UseDefault();
        Repositories().Create().UseDefault();
        Repositories().Update().UseDefault();
        Repositories().Delete().UseDefault();

        // Issues
        Issues().Get().UseDefault();
        Issues().List().UseDefault();
        Issues().Create().UseDefault();
        Issues().Update().UseDefault();

        // Pull Requests
        PullRequests().Get().UseDefault();
        PullRequests().List().UseDefault();
        PullRequests().Create().UseDefault();
        PullRequests().Update().UseDefault();

        // Users
        Users().Get().UseDefault();
        Users().GetAuthenticated().UseDefault();

        // Organizations
        Organizations().Get().UseDefault();
        Organizations().ListForAuthenticatedUser().UseDefault();
        Organizations().ListForUser().UseDefault();

        // Commits
        Commits().Get().UseDefault();
        Commits().List().UseDefault();

        // Branches
        Branches().Get().UseDefault();
        Branches().List().UseDefault();

        // Releases
        Releases().Get().UseDefault();
        Releases().List().UseDefault();
        Releases().Create().UseDefault();

        // Actions
        Actions().GetWorkflowRun().UseDefault();
        Actions().ListWorkflowRuns().UseDefault();

        // Git Refs
        GitRefs().Get().UseDefault();
        GitRefs().List().UseDefault();
        GitRefs().Create().UseDefault();

        // Search
        Search().Repositories().UseDefault();
        Search().Issues().UseDefault();
        Search().Code().UseDefault();

        return this;
    }

    /// <summary>
    /// Configures Gaussian latency simulation for all responses from this GitHub mock.
    /// </summary>
    /// <param name="meanMs">The mean delay in milliseconds.</param>
    /// <param name="stdDevMs">The standard deviation of the delay in milliseconds.</param>
    /// <returns>This <see cref="GitHubMock"/> instance for method chaining.</returns>
    public GitHubMock WithLatency(int meanMs, int stdDevMs)
    {
        apiSimulator.WithLatency(meanMs, stdDevMs);
        return this;
    }

    /// <summary>
    /// Builds the mock server and returns it for starting.
    /// </summary>
    public APISimulator Build() => apiSimulator;

    // Delegate common APISimulator operations
    public Task StartAsync(CancellationToken cancellationToken = default) => apiSimulator.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => apiSimulator.StopAsync(cancellationToken);
    public HttpClient CreateClient() => apiSimulator.CreateClient();
    public bool IsRunning => apiSimulator.IsRunning;
}

/// <summary>
/// Provides access to GitHub route builders from an existing APISimulator.
/// </summary>
public sealed class GitHubRouteBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubRouteBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures GitHub repository endpoints.
    /// </summary>
    public GitHubRepositoriesBuilder Repositories() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub issue endpoints.
    /// </summary>
    public GitHubIssuesBuilder Issues() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub pull request endpoints.
    /// </summary>
    public GitHubPullRequestsBuilder PullRequests() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub user endpoints.
    /// </summary>
    public GitHubUsersBuilder Users() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub organization endpoints.
    /// </summary>
    public GitHubOrganizationsBuilder Organizations() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub commit endpoints.
    /// </summary>
    public GitHubCommitsBuilder Commits() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub branch endpoints.
    /// </summary>
    public GitHubBranchesBuilder Branches() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub release endpoints.
    /// </summary>
    public GitHubReleasesBuilder Releases() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub Actions endpoints.
    /// </summary>
    public GitHubActionsBuilder Actions() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub Git references endpoints.
    /// </summary>
    public GitHubGitRefsBuilder GitRefs() => new(apiSimulator);

    /// <summary>
    /// Configures GitHub search endpoints.
    /// </summary>
    public GitHubSearchBuilder Search() => new(apiSimulator);
}
