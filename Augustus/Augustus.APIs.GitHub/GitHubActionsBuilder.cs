namespace Augustus.APIs.GitHub;

using Augustus;

/// <summary>
/// Builder for configuring GitHub Actions API endpoints.
/// </summary>
public sealed class GitHubActionsBuilder
{
    private readonly APISimulator apiSimulator;

    internal GitHubActionsBuilder(APISimulator apiSimulator)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/actions/runs/{run_id} endpoint.
    /// </summary>
    public GitHubResourceConfigurer GetWorkflowRun()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/actions/runs/{run_id}", "GET", "workflow_run");
    }

    /// <summary>
    /// Configures the GET /repos/{owner}/{repo}/actions/runs endpoint (list workflow runs).
    /// </summary>
    public GitHubResourceConfigurer ListWorkflowRuns()
    {
        return new GitHubResourceConfigurer(apiSimulator, "/repos/{owner}/{repo}/actions/runs", "GET", "workflow_run_list");
    }
}
