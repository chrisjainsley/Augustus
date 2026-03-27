namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateWorkflowRun()
    {
        var id = GetRandomInt(100000, 999999);
        var actor = GenerateUserElement(DefaultLogin);
        var headSha = GenerateRandomSha();
        var treeSha = GenerateRandomSha();
        var now = GenerateTimestamp();
        var workflowId = GetRandomInt(1000, 9999);
        var checkSuiteId = GetRandomInt(100000, 999999);
        var runNumber = GetRandomInt(1, 200);

        var run = new
        {
            id,
            name = "CI",
            node_id = GenerateNodeId("WR"),
            check_suite_id = checkSuiteId,
            check_suite_node_id = GenerateNodeId("CS"),
            head_branch = "main",
            head_sha = headSha,
            path = ".github/workflows/ci.yml",
            run_number = runNumber,
            run_attempt = 1,
            @event = "push",
            status = "completed",
            conclusion = "success",
            workflow_id = workflowId,
            url = $"https://api.github.com/repos/{DefaultFullName}/actions/runs/{id}",
            html_url = $"https://github.com/{DefaultFullName}/actions/runs/{id}",
            pull_requests = Array.Empty<object>(),
            created_at = now,
            updated_at = now,
            run_started_at = now,
            actor,
            triggering_actor = actor,
            jobs_url = $"https://api.github.com/repos/{DefaultFullName}/actions/runs/{id}/jobs",
            logs_url = $"https://api.github.com/repos/{DefaultFullName}/actions/runs/{id}/logs",
            check_suite_url = $"https://api.github.com/repos/{DefaultFullName}/check-suites/{checkSuiteId}",
            artifacts_url = $"https://api.github.com/repos/{DefaultFullName}/actions/runs/{id}/artifacts",
            cancel_url = $"https://api.github.com/repos/{DefaultFullName}/actions/runs/{id}/cancel",
            rerun_url = $"https://api.github.com/repos/{DefaultFullName}/actions/runs/{id}/rerun",
            previous_attempt_url = (string?)null,
            workflow_url = $"https://api.github.com/repos/{DefaultFullName}/actions/workflows/{workflowId}",
            repository = new
            {
                id = GetRandomInt(100000, 999999),
                node_id = GenerateNodeId("R"),
                name = DefaultRepo,
                full_name = DefaultFullName,
                @private = false,
                html_url = $"https://github.com/{DefaultFullName}",
                url = $"https://api.github.com/repos/{DefaultFullName}"
            },
            head_repository = new
            {
                id = GetRandomInt(100000, 999999),
                node_id = GenerateNodeId("R"),
                name = DefaultRepo,
                full_name = DefaultFullName,
                @private = false,
                html_url = $"https://github.com/{DefaultFullName}",
                url = $"https://api.github.com/repos/{DefaultFullName}"
            },
            head_commit = new
            {
                id = headSha,
                tree_id = treeSha,
                message = "Test commit",
                timestamp = now,
                author = new { name = "Octocat", email = "octocat@github.com" },
                committer = new { name = "Octocat", email = "octocat@github.com" }
            },
            display_title = "CI"
        };

        return JsonSerializer.Serialize(run, SerializerOptions);
    }

    private static string GenerateWorkflowRunList(int count = 3)
    {
        var items = Enumerable.Range(0, count)
            .Select(_ => JsonSerializer.Deserialize<JsonElement>(GenerateWorkflowRun()))
            .ToArray();

        var result = new
        {
            total_count = count,
            workflow_runs = items
        };

        return JsonSerializer.Serialize(result, SerializerOptions);
    }
}
