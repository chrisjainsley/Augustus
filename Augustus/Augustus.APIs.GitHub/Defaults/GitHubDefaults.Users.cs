namespace Augustus.APIs.GitHub;

using System.Text.Json;
using System.Text.Json.Nodes;

internal static partial class GitHubDefaults
{
    private static string GeneratePublicUser(string? login = null)
    {
        var node = JsonNode.Parse(GenerateUser(login))!.AsObject();
        var now = GenerateTimestamp();

        node["name"] = (string?)node["login"];
        node["company"] = null;
        node["blog"] = "";
        node["location"] = null;
        node["email"] = null;
        node["hireable"] = null;
        node["bio"] = "A test user bio.";
        node["twitter_username"] = null;
        node["public_repos"] = GetRandomInt(5, 50);
        node["public_gists"] = GetRandomInt(0, 10);
        node["followers"] = GetRandomInt(0, 200);
        node["following"] = GetRandomInt(0, 100);
        node["created_at"] = now;
        node["updated_at"] = now;

        return node.ToJsonString(SerializerOptions);
    }

    private static string GenerateAuthenticatedUser()
    {
        var node = JsonNode.Parse(GeneratePublicUser())!.AsObject();

        node["private_gists"] = GetRandomInt(0, 5);
        node["total_private_repos"] = GetRandomInt(0, 10);
        node["owned_private_repos"] = GetRandomInt(0, 10);
        node["disk_usage"] = GetRandomInt(1000, 100000);
        node["collaborators"] = GetRandomInt(0, 5);
        node["two_factor_authentication"] = true;
        node["plan"] = new JsonObject
        {
            ["name"] = "free",
            ["space"] = 976562499,
            ["private_repos"] = 10000,
            ["collaborators"] = 0
        };

        return node.ToJsonString(SerializerOptions);
    }
}
