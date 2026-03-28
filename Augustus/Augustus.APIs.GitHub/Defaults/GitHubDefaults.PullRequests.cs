namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GeneratePullRequest()
    {
        var id = GetRandomInt(100000, 999999);
        var number = GetRandomInt(1, 500);
        var user = GenerateUserElement(DefaultLogin);
        var now = GenerateTimestamp();
        var headSha = GenerateRandomSha();
        var baseSha = GenerateRandomSha();

        var pr = new
        {
            url = $"https://api.github.com/repos/{DefaultFullName}/pulls/{number}",
            id,
            node_id = GenerateNodeId("PR"),
            html_url = $"https://github.com/{DefaultFullName}/pull/{number}",
            diff_url = $"https://github.com/{DefaultFullName}/pull/{number}.diff",
            patch_url = $"https://github.com/{DefaultFullName}/pull/{number}.patch",
            issue_url = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}",
            commits_url = $"https://api.github.com/repos/{DefaultFullName}/pulls/{number}/commits",
            review_comments_url = $"https://api.github.com/repos/{DefaultFullName}/pulls/{number}/comments",
            review_comment_url = $"https://api.github.com/repos/{DefaultFullName}/pulls/comments{{/number}}",
            comments_url = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}/comments",
            statuses_url = $"https://api.github.com/repos/{DefaultFullName}/statuses/{headSha}",
            number,
            state = "open",
            locked = false,
            title = $"Test pull request {GetRandomInt(100, 999)}",
            body = "This is a test pull request body.",
            user,
            labels = Array.Empty<object>(),
            assignees = Array.Empty<object>(),
            requested_reviewers = Array.Empty<object>(),
            requested_teams = Array.Empty<object>(),
            milestone = (object?)null,
            author_association = "OWNER",
            draft = false,
            merged = false,
            mergeable = true,
            rebaseable = true,
            mergeable_state = "clean",
            merged_by = (object?)null,
            merge_commit_sha = (string?)null,
            comments = 0,
            review_comments = 0,
            commits = 1,
            additions = GetRandomInt(1, 100),
            deletions = GetRandomInt(0, 50),
            changed_files = GetRandomInt(1, 10),
            maintainer_can_modify = false,
            head = new
            {
                label = $"{DefaultLogin}:feature",
                @ref = "feature",
                sha = headSha,
                user
            },
            @base = new
            {
                label = $"{DefaultLogin}:main",
                @ref = "main",
                sha = baseSha,
                user
            },
            created_at = now,
            updated_at = now,
            closed_at = (string?)null,
            merged_at = (string?)null,
            auto_merge = (object?)null,
            _links = new
            {
                self = new { href = $"https://api.github.com/repos/{DefaultFullName}/pulls/{number}" },
                html = new { href = $"https://github.com/{DefaultFullName}/pull/{number}" },
                issue = new { href = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}" },
                comments = new { href = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}/comments" },
                review_comments = new { href = $"https://api.github.com/repos/{DefaultFullName}/pulls/{number}/comments" },
                review_comment = new { href = $"https://api.github.com/repos/{DefaultFullName}/pulls/comments{{/number}}" },
                commits = new { href = $"https://api.github.com/repos/{DefaultFullName}/pulls/{number}/commits" },
                statuses = new { href = $"https://api.github.com/repos/{DefaultFullName}/statuses/{headSha}" }
            }
        };

        return JsonSerializer.Serialize(pr, SerializerOptions);
    }
}
