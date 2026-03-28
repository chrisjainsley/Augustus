namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateRepository()
    {
        var id = GetRandomInt(100000, 999999);
        var name = $"test-repo-{GetRandomInt(100, 999)}";
        var fullName = $"{DefaultLogin}/{name}";
        var owner = GenerateUserElement(DefaultLogin);
        var now = GenerateTimestamp();

        var repo = new
        {
            id,
            node_id = GenerateNodeId("R"),
            name,
            full_name = fullName,
            @private = false,
            owner,
            html_url = $"https://github.com/{fullName}",
            description = "A test repository",
            fork = false,
            url = $"https://api.github.com/repos/{fullName}",
            forks_url = $"https://api.github.com/repos/{fullName}/forks",
            keys_url = $"https://api.github.com/repos/{fullName}/keys{{/key_id}}",
            collaborators_url = $"https://api.github.com/repos/{fullName}/collaborators{{/collaborator}}",
            teams_url = $"https://api.github.com/repos/{fullName}/teams",
            hooks_url = $"https://api.github.com/repos/{fullName}/hooks",
            issue_events_url = $"https://api.github.com/repos/{fullName}/issues/events{{/number}}",
            events_url = $"https://api.github.com/repos/{fullName}/events",
            assignees_url = $"https://api.github.com/repos/{fullName}/assignees{{/user}}",
            branches_url = $"https://api.github.com/repos/{fullName}/branches{{/branch}}",
            tags_url = $"https://api.github.com/repos/{fullName}/tags",
            blobs_url = $"https://api.github.com/repos/{fullName}/git/blobs{{/sha}}",
            git_tags_url = $"https://api.github.com/repos/{fullName}/git/tags{{/sha}}",
            git_refs_url = $"https://api.github.com/repos/{fullName}/git/refs{{/sha}}",
            trees_url = $"https://api.github.com/repos/{fullName}/git/trees{{/sha}}",
            statuses_url = $"https://api.github.com/repos/{fullName}/statuses/{GenerateRandomSha()}",
            languages_url = $"https://api.github.com/repos/{fullName}/languages",
            stargazers_url = $"https://api.github.com/repos/{fullName}/stargazers",
            contributors_url = $"https://api.github.com/repos/{fullName}/contributors",
            subscribers_url = $"https://api.github.com/repos/{fullName}/subscribers",
            subscription_url = $"https://api.github.com/repos/{fullName}/subscription",
            commits_url = $"https://api.github.com/repos/{fullName}/commits{{/sha}}",
            git_commits_url = $"https://api.github.com/repos/{fullName}/git/commits{{/sha}}",
            comments_url = $"https://api.github.com/repos/{fullName}/comments{{/number}}",
            issue_comment_url = $"https://api.github.com/repos/{fullName}/issues/comments{{/number}}",
            contents_url = $"https://api.github.com/repos/{fullName}/contents/{{+path}}",
            compare_url = $"https://api.github.com/repos/{fullName}/compare/{{base}}...{{head}}",
            merges_url = $"https://api.github.com/repos/{fullName}/merges",
            archive_url = $"https://api.github.com/repos/{fullName}/{{archive_format}}{{/ref}}",
            downloads_url = $"https://api.github.com/repos/{fullName}/downloads",
            issues_url = $"https://api.github.com/repos/{fullName}/issues{{/number}}",
            pulls_url = $"https://api.github.com/repos/{fullName}/pulls{{/number}}",
            milestones_url = $"https://api.github.com/repos/{fullName}/milestones{{/number}}",
            notifications_url = $"https://api.github.com/repos/{fullName}/notifications{{?since,all,participating}}",
            labels_url = $"https://api.github.com/repos/{fullName}/labels{{/name}}",
            releases_url = $"https://api.github.com/repos/{fullName}/releases{{/id}}",
            deployments_url = $"https://api.github.com/repos/{fullName}/deployments",
            created_at = now,
            updated_at = now,
            pushed_at = now,
            git_url = $"git://github.com/{fullName}.git",
            ssh_url = $"git@github.com:{fullName}.git",
            clone_url = $"https://github.com/{fullName}.git",
            svn_url = $"https://github.com/{fullName}",
            homepage = (string?)null,
            size = GetRandomInt(100, 10000),
            stargazers_count = GetRandomInt(0, 500),
            watchers_count = GetRandomInt(0, 500),
            language = "C#",
            has_issues = true,
            has_projects = true,
            has_downloads = true,
            has_wiki = true,
            has_pages = false,
            has_discussions = false,
            forks_count = GetRandomInt(0, 50),
            mirror_url = (string?)null,
            archived = false,
            disabled = false,
            open_issues_count = GetRandomInt(0, 20),
            license = (object?)null,
            allow_forking = true,
            is_template = false,
            web_commit_signoff_required = false,
            topics = Array.Empty<string>(),
            visibility = "public",
            forks = GetRandomInt(0, 50),
            open_issues = GetRandomInt(0, 20),
            watchers = GetRandomInt(0, 500),
            default_branch = "main",
            permissions = new
            {
                admin = true,
                maintain = true,
                push = true,
                triage = true,
                pull = true
            }
        };

        return JsonSerializer.Serialize(repo, SerializerOptions);
    }
}
