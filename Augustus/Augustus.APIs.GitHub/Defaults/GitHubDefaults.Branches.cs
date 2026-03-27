namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateBranch()
    {
        var sha = GenerateRandomSha();
        var branchName = "main";

        var branch = new
        {
            name = branchName,
            commit = new
            {
                sha,
                url = $"https://api.github.com/repos/{DefaultFullName}/commits/{sha}"
            },
            @protected = false,
            protection = new
            {
                enabled = false,
                required_status_checks = (object?)null
            },
            protection_url = $"https://api.github.com/repos/{DefaultFullName}/branches/{branchName}/protection"
        };

        return JsonSerializer.Serialize(branch, SerializerOptions);
    }
}
