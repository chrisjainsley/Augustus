namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateIssue()
    {
        var id = GetRandomInt(100000, 999999);
        var number = GetRandomInt(1, 500);
        var user = GenerateUserElement(DefaultLogin);
        var now = GenerateTimestamp();

        var reactionsUrl = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}/reactions";
        var reactions = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["url"] = reactionsUrl,
                ["total_count"] = 0,
                ["+1"] = 0,
                ["-1"] = 0,
                ["laugh"] = 0,
                ["confused"] = 0,
                ["heart"] = 0,
                ["hooray"] = 0,
                ["eyes"] = 0,
                ["rocket"] = 0
            }, SerializerOptions));

        var issue = new
        {
            id,
            node_id = GenerateNodeId("I"),
            url = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}",
            repository_url = $"https://api.github.com/repos/{DefaultFullName}",
            labels_url = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}/labels{{/name}}",
            comments_url = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}/comments",
            events_url = $"https://api.github.com/repos/{DefaultFullName}/issues/{number}/events",
            html_url = $"https://github.com/{DefaultFullName}/issues/{number}",
            number,
            state = "open",
            state_reason = (string?)null,
            title = $"Test issue {GetRandomInt(100, 999)}",
            body = "This is a test issue body.",
            user,
            labels = Array.Empty<object>(),
            assignees = Array.Empty<object>(),
            milestone = (object?)null,
            locked = false,
            active_lock_reason = (string?)null,
            comments = GetRandomInt(0, 10),
            closed_at = (string?)null,
            created_at = now,
            updated_at = now,
            author_association = "NONE",
            reactions
        };

        return JsonSerializer.Serialize(issue, SerializerOptions);
    }
}
