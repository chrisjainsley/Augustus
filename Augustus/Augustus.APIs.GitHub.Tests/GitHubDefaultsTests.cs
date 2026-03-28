using System.Text.Json;
using Augustus.APIs.GitHub;

namespace Augustus.APIs.GitHub.Tests;

public class GitHubDefaultsTests
{
    [Theory]
    [InlineData("repository", "full_name")]
    [InlineData("issue", "number")]
    [InlineData("pull_request", "number")]
    [InlineData("user", "login")]
    [InlineData("user_authenticated", "login")]
    [InlineData("organization", "login")]
    [InlineData("commit", "sha")]
    [InlineData("branch", "name")]
    [InlineData("release", "tag_name")]
    [InlineData("workflow_run", "name")]
    [InlineData("git_ref", "ref")]
    public void GetDefaultResponse_SingleResource_ShouldHaveRequiredField(string resourceType, string requiredField)
    {
        var json = GitHubDefaults.GetDefaultResponse(resourceType);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty(requiredField, out _).Should().BeTrue(
            $"resource '{resourceType}' should have field '{requiredField}'");
    }

    [Theory]
    [InlineData("repository_list")]
    [InlineData("issue_list")]
    [InlineData("pull_request_list")]
    [InlineData("commit_list")]
    [InlineData("branch_list")]
    [InlineData("release_list")]
    [InlineData("git_ref_list")]
    public void GetDefaultResponse_List_ShouldBeBareJsonArray(string resourceType)
    {
        var json = GitHubDefaults.GetDefaultResponse(resourceType);
        var doc = JsonDocument.Parse(json);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultResponse_OrganizationList_ShouldBeBareJsonArray()
    {
        var json = GitHubDefaults.GetDefaultResponse("organization_list");
        var doc = JsonDocument.Parse(json);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultResponse_WorkflowRunList_ShouldBeWrappedObject()
    {
        var json = GitHubDefaults.GetDefaultResponse("workflow_run_list");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Object);
        root.GetProperty("total_count").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("workflow_runs").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("search_repositories")]
    [InlineData("search_issues")]
    [InlineData("search_code")]
    public void GetDefaultResponse_Search_ShouldHaveSearchStructure(string resourceType)
    {
        var json = GitHubDefaults.GetDefaultResponse(resourceType);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("total_count").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("incomplete_results").GetBoolean().Should().BeFalse();
        root.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultResponse_Repository_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("repository");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("node_id").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("name").GetString().Should().StartWith("test-repo");
        root.GetProperty("full_name").GetString().Should().Contain("/");
        root.GetProperty("owner").GetProperty("login").GetString().Should().Be("octocat");
        root.GetProperty("has_downloads").GetBoolean().Should().BeTrue();
        root.GetProperty("default_branch").GetString().Should().Be("main");
        root.GetProperty("visibility").GetString().Should().Be("public");
        root.GetProperty("permissions").GetProperty("admin").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void GetDefaultResponse_Issue_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("issue");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("number").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("state").GetString().Should().Be("open");
        root.GetProperty("title").GetString().Should().StartWith("Test issue");
        root.GetProperty("user").GetProperty("login").GetString().Should().Be("octocat");
        root.GetProperty("reactions").GetProperty("url").GetString().Should().Contain("/reactions");
        root.GetProperty("reactions").GetProperty("+1").GetInt32().Should().Be(0);
        root.GetProperty("reactions").GetProperty("-1").GetInt32().Should().Be(0);
    }

    [Fact]
    public void GetDefaultResponse_PullRequest_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("pull_request");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("number").GetInt32().Should().BeGreaterThan(0);
        root.GetProperty("state").GetString().Should().Be("open");
        root.GetProperty("draft").GetBoolean().Should().BeFalse();
        root.GetProperty("merged").GetBoolean().Should().BeFalse();
        root.GetProperty("mergeable").GetBoolean().Should().BeTrue();
        root.GetProperty("head").GetProperty("ref").GetString().Should().Be("feature");
        root.GetProperty("base").GetProperty("ref").GetString().Should().Be("main");
        root.GetProperty("head").GetProperty("sha").GetString().Should().HaveLength(40);
        root.GetProperty("_links").GetProperty("self").GetProperty("href").GetString().Should().Contain("/pulls/");
    }

    [Fact]
    public void GetDefaultResponse_AuthenticatedUser_ShouldHaveExtraFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("user_authenticated");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("login").GetString().Should().Be("octocat");
        root.GetProperty("two_factor_authentication").GetBoolean().Should().BeTrue();
        root.GetProperty("plan").GetProperty("name").GetString().Should().Be("free");
    }

    [Fact]
    public void GetDefaultResponse_Commit_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("commit");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("sha").GetString().Should().HaveLength(40);
        root.GetProperty("commit").GetProperty("message").GetString().Should().StartWith("Test commit message");
        root.GetProperty("commit").GetProperty("verification").GetProperty("verified").GetBoolean().Should().BeFalse();
        root.GetProperty("parents").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultResponse_Branch_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("branch");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("name").GetString().Should().Be("main");
        root.GetProperty("commit").GetProperty("sha").GetString().Should().HaveLength(40);
        root.GetProperty("protected").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GetDefaultResponse_Release_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("release");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("tag_name").GetString().Should().StartWith("v");
        root.GetProperty("target_commitish").GetString().Should().Be("main");
        root.GetProperty("draft").GetBoolean().Should().BeFalse();
        root.GetProperty("prerelease").GetBoolean().Should().BeFalse();
        root.GetProperty("author").GetProperty("login").GetString().Should().Be("octocat");
    }

    [Fact]
    public void GetDefaultResponse_WorkflowRun_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("workflow_run");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("name").GetString().Should().Be("CI");
        root.GetProperty("status").GetString().Should().Be("completed");
        root.GetProperty("conclusion").GetString().Should().Be("success");
        root.GetProperty("head_sha").GetString().Should().HaveLength(40);
        root.GetProperty("actor").GetProperty("login").GetString().Should().Be("octocat");
        root.GetProperty("repository").GetProperty("full_name").GetString().Should().Contain("/");
        root.GetProperty("head_repository").GetProperty("full_name").GetString().Should().Contain("/");
    }

    [Fact]
    public void GetDefaultResponse_GitRef_ShouldHaveRequiredFields()
    {
        var json = GitHubDefaults.GetDefaultResponse("git_ref");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("ref").GetString().Should().Be("refs/heads/main");
        root.GetProperty("object").GetProperty("type").GetString().Should().Be("commit");
        root.GetProperty("object").GetProperty("sha").GetString().Should().HaveLength(40);
    }

    [Fact]
    public void GetDefaultResponse_RepositoryDeleted_ShouldReturnEmpty()
    {
        var json = GitHubDefaults.GetDefaultResponse("repository_deleted");
        json.Should().BeEmpty();
    }

    [Fact]
    public void GetDefaultResponse_UnknownResource_ShouldReturnEmptyJson()
    {
        var json = GitHubDefaults.GetDefaultResponse("unknown_resource");
        json.Should().Be("{}");
    }
}
