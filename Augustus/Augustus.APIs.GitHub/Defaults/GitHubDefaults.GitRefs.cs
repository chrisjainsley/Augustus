namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateGitRef()
    {
        var sha = GenerateRandomSha();

        var gitRef = new
        {
            @ref = "refs/heads/main",
            node_id = GenerateNodeId("REF"),
            url = $"https://api.github.com/repos/{DefaultFullName}/git/refs/heads/main",
            @object = new
            {
                type = "commit",
                sha,
                url = $"https://api.github.com/repos/{DefaultFullName}/git/commits/{sha}"
            }
        };

        return JsonSerializer.Serialize(gitRef, SerializerOptions);
    }
}
