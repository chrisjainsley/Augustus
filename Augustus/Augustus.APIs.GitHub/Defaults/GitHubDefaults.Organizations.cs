namespace Augustus.APIs.GitHub;

using System.Text.Json;

internal static partial class GitHubDefaults
{
    private static string GenerateOrganization(string? orgLogin = null)
    {
        orgLogin ??= "test-org";
        var id = GetRandomInt(10000, 99999);
        var now = GenerateTimestamp();

        var org = new
        {
            login = orgLogin,
            id,
            node_id = GenerateNodeId("O"),
            url = $"https://api.github.com/orgs/{orgLogin}",
            repos_url = $"https://api.github.com/orgs/{orgLogin}/repos",
            events_url = $"https://api.github.com/orgs/{orgLogin}/events",
            hooks_url = $"https://api.github.com/orgs/{orgLogin}/hooks",
            issues_url = $"https://api.github.com/orgs/{orgLogin}/issues",
            members_url = $"https://api.github.com/orgs/{orgLogin}/members{{/member}}",
            public_members_url = $"https://api.github.com/orgs/{orgLogin}/public_members{{/member}}",
            avatar_url = $"https://avatars.githubusercontent.com/u/{id}",
            description = "A test organization.",
            name = orgLogin,
            company = (string?)null,
            blog = "",
            location = (string?)null,
            email = (string?)null,
            twitter_username = (string?)null,
            is_verified = false,
            has_organization_projects = true,
            has_repository_projects = true,
            public_repos = GetRandomInt(5, 50),
            public_gists = 0,
            followers = GetRandomInt(0, 100),
            following = 0,
            html_url = $"https://github.com/{orgLogin}",
            type = "Organization",
            created_at = now,
            updated_at = now
        };

        return JsonSerializer.Serialize(org, SerializerOptions);
    }

    private static string GenerateOrganizationSimple(string? orgLogin = null)
    {
        orgLogin ??= "test-org";
        var id = GetRandomInt(10000, 99999);

        var org = new
        {
            login = orgLogin,
            id,
            node_id = GenerateNodeId("O"),
            url = $"https://api.github.com/orgs/{orgLogin}",
            repos_url = $"https://api.github.com/orgs/{orgLogin}/repos",
            events_url = $"https://api.github.com/orgs/{orgLogin}/events",
            hooks_url = $"https://api.github.com/orgs/{orgLogin}/hooks",
            issues_url = $"https://api.github.com/orgs/{orgLogin}/issues",
            members_url = $"https://api.github.com/orgs/{orgLogin}/members{{/member}}",
            public_members_url = $"https://api.github.com/orgs/{orgLogin}/public_members{{/member}}",
            avatar_url = $"https://avatars.githubusercontent.com/u/{id}",
            description = "A test organization."
        };

        return JsonSerializer.Serialize(org, SerializerOptions);
    }
}
