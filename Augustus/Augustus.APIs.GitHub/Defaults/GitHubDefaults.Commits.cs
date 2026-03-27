namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateCommit()
    {
        var sha = GenerateRandomSha();
        var treeSha = GenerateRandomSha();
        var parentSha = GenerateRandomSha();
        var author = GenerateUserElement(DefaultLogin);
        var now = GenerateTimestamp();

        var commit = new
        {
            url = $"https://api.github.com/repos/{DefaultFullName}/commits/{sha}",
            sha,
            node_id = GenerateNodeId("C"),
            html_url = $"https://github.com/{DefaultFullName}/commit/{sha}",
            comments_url = $"https://api.github.com/repos/{DefaultFullName}/commits/{sha}/comments",
            commit = new
            {
                url = $"https://api.github.com/repos/{DefaultFullName}/git/commits/{sha}",
                message = $"Test commit message {GetRandomInt(100, 999)}",
                comment_count = 0,
                author = new
                {
                    name = "Octocat",
                    email = "octocat@github.com",
                    date = now
                },
                committer = new
                {
                    name = "Octocat",
                    email = "octocat@github.com",
                    date = now
                },
                tree = new
                {
                    sha = treeSha,
                    url = $"https://api.github.com/repos/{DefaultFullName}/git/trees/{treeSha}"
                },
                verification = new
                {
                    verified = false,
                    reason = "unsigned",
                    signature = (string?)null,
                    payload = (string?)null
                }
            },
            author,
            committer = author,
            parents = new[]
            {
                new
                {
                    sha = parentSha,
                    url = $"https://api.github.com/repos/{DefaultFullName}/commits/{parentSha}",
                    html_url = $"https://github.com/{DefaultFullName}/commit/{parentSha}"
                }
            }
        };

        return JsonSerializer.Serialize(commit, SerializerOptions);
    }
}
