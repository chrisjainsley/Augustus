namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateSearchCodeItem()
    {
        var sha = GenerateRandomSha();
        var fileName = $"TestFile{GetRandomInt(1, 100)}.cs";
        var filePath = $"src/{fileName}";

        var item = new
        {
            name = fileName,
            path = filePath,
            sha,
            url = $"https://api.github.com/repos/{DefaultFullName}/contents/{filePath}?ref=main",
            git_url = $"https://api.github.com/repos/{DefaultFullName}/git/blobs/{sha}",
            html_url = $"https://github.com/{DefaultFullName}/blob/main/{filePath}",
            repository = new
            {
                id = GetRandomInt(100000, 999999),
                node_id = GenerateNodeId("R"),
                name = DefaultRepo,
                full_name = DefaultFullName,
                @private = false,
                html_url = $"https://github.com/{DefaultFullName}",
                description = "A test repository",
                url = $"https://api.github.com/repos/{DefaultFullName}"
            },
            score = 1.0
        };

        return JsonSerializer.Serialize(item, SerializerOptions);
    }
}
