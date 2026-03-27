namespace Augustus.APIs.GitHub;

using System.Text.Json;

/// <summary>
/// Provides default realistic GitHub API responses.
/// </summary>
internal static partial class GitHubDefaults
{
    private const string DefaultLogin = "octocat";
    private const string DefaultRepo = "test-repo";
    private const string DefaultFullName = DefaultLogin + "/" + DefaultRepo;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public static string GetDefaultResponse(string resourceType)
    {
        return resourceType switch
        {
            "repository" => GenerateRepository(),
            "repository_list" => GenerateList(GenerateRepository),
            "repository_deleted" => "",
            "issue" => GenerateIssue(),
            "issue_list" => GenerateList(GenerateIssue),
            "pull_request" => GeneratePullRequest(),
            "pull_request_list" => GenerateList(GeneratePullRequest),
            "user" => GeneratePublicUser(),
            "user_authenticated" => GenerateAuthenticatedUser(),
            "organization" => GenerateOrganization(),
            "organization_list" => GenerateList(() => GenerateOrganizationSimple()),
            "commit" => GenerateCommit(),
            "commit_list" => GenerateList(GenerateCommit),
            "branch" => GenerateBranch(),
            "branch_list" => GenerateList(GenerateBranch),
            "release" => GenerateRelease(),
            "release_list" => GenerateList(GenerateRelease),
            "workflow_run" => GenerateWorkflowRun(),
            "workflow_run_list" => GenerateWorkflowRunList(),
            "git_ref" => GenerateGitRef(),
            "git_ref_list" => GenerateList(GenerateGitRef),
            "search_repositories" => GenerateSearchResult(GenerateRepository),
            "search_issues" => GenerateSearchResult(GenerateIssue),
            "search_code" => GenerateSearchResult(GenerateSearchCodeItem),
            _ => "{}"
        };
    }

    private static string GenerateList(Func<string> generateItem, int count = 3)
    {
        var items = Enumerable.Range(0, count)
            .Select(_ => JsonSerializer.Deserialize<JsonElement>(generateItem()))
            .ToArray();

        return JsonSerializer.Serialize(items, SerializerOptions);
    }

    private static string GenerateSearchResult(Func<string> generateItem, int count = 3)
    {
        var items = Enumerable.Range(0, count)
            .Select(_ => JsonSerializer.Deserialize<JsonElement>(generateItem()))
            .ToArray();

        var result = new
        {
            total_count = count,
            incomplete_results = false,
            items = items
        };

        return JsonSerializer.Serialize(result, SerializerOptions);
    }

    internal static int GetRandomInt(int minValue, int maxValue)
        => Random.Shared.Next(minValue, maxValue);

    internal static string GenerateRandomSha()
    {
        const string hexChars = "0123456789abcdef";
        return new string(Enumerable.Range(0, 40).Select(_ => hexChars[GetRandomInt(0, hexChars.Length)]).ToArray());
    }

    internal static string GenerateNodeId(string prefix)
    {
        var raw = $"{prefix}_{GetRandomInt(100000, 999999)}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw)).TrimEnd('=');
    }

    internal static string GenerateTimestamp(int daysAgo = 0)
    {
        return DateTimeOffset.UtcNow.AddDays(-daysAgo).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    internal static JsonElement GenerateUserElement(string? login = null)
        => JsonSerializer.Deserialize<JsonElement>(GenerateUser(login));

    internal static string GenerateUser(string? login = null)
    {
        login ??= DefaultLogin;
        var id = GetRandomInt(10000, 99999);
        var user = new
        {
            login,
            id,
            node_id = GenerateNodeId("U"),
            avatar_url = $"https://avatars.githubusercontent.com/u/{id}",
            gravatar_id = "",
            url = $"https://api.github.com/users/{login}",
            html_url = $"https://github.com/{login}",
            followers_url = $"https://api.github.com/users/{login}/followers",
            following_url = $"https://api.github.com/users/{login}/following{{/other_user}}",
            gists_url = $"https://api.github.com/users/{login}/gists{{/gist_id}}",
            starred_url = $"https://api.github.com/users/{login}/starred{{/owner}}{{/repo}}",
            subscriptions_url = $"https://api.github.com/users/{login}/subscriptions",
            organizations_url = $"https://api.github.com/users/{login}/orgs",
            repos_url = $"https://api.github.com/users/{login}/repos",
            events_url = $"https://api.github.com/users/{login}/events{{/privacy}}",
            received_events_url = $"https://api.github.com/users/{login}/received_events",
            type = "User",
            site_admin = false
        };
        return JsonSerializer.Serialize(user, SerializerOptions);
    }
}
