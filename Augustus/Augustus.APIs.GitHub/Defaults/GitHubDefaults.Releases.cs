namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateRelease()
    {
        var id = GetRandomInt(100000, 999999);
        var author = GenerateUserElement(DefaultLogin);
        var now = GenerateTimestamp();
        var tagName = $"v{GetRandomInt(1, 5)}.{GetRandomInt(0, 9)}.{GetRandomInt(0, 9)}";

        var release = new
        {
            url = $"https://api.github.com/repos/{DefaultFullName}/releases/{id}",
            html_url = $"https://github.com/{DefaultFullName}/releases/tag/{tagName}",
            assets_url = $"https://api.github.com/repos/{DefaultFullName}/releases/{id}/assets",
            upload_url = $"https://uploads.github.com/repos/{DefaultFullName}/releases/{id}/assets{{?name,label}}",
            tarball_url = $"https://api.github.com/repos/{DefaultFullName}/tarball/{tagName}",
            zipball_url = $"https://api.github.com/repos/{DefaultFullName}/zipball/{tagName}",
            id,
            node_id = GenerateNodeId("RE"),
            tag_name = tagName,
            target_commitish = "main",
            name = tagName,
            body = $"Release {tagName}",
            draft = false,
            prerelease = false,
            created_at = now,
            published_at = now,
            author,
            assets = Array.Empty<object>()
        };

        return JsonSerializer.Serialize(release, SerializerOptions);
    }
}
