using Augustus;
using Augustus.APIs.GitHub;
using System.Net;

namespace Augustus.APIs.GitHub.Tests;

public class GitHubIntegrationTests
{
    [Fact]
    public async Task GitHubMock_WithDefaultResponse_ShouldReturnRealisticRepository()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Repositories().Get().UseDefault();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetStringAsync("/repos/octocat/hello-world");

            response.Should().Contain("\"full_name\"");
            response.Should().Contain("\"owner\"");
            response.Should().Contain("\"default_branch\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_MultipleEndpoints_ShouldAllWork()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Repositories().Get().UseDefault();
        simulator.Issues().List().UseDefault();
        simulator.PullRequests().Get().UseDefault();
        simulator.Users().GetAuthenticated().UseDefault();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            var repo = await client.GetStringAsync("/repos/octocat/hello-world");
            repo.Should().Contain("\"full_name\"");

            var issues = await client.GetStringAsync("/repos/octocat/hello-world/issues");
            issues.Should().StartWith("[");

            var pr = await client.GetStringAsync("/repos/octocat/hello-world/pulls/1");
            pr.Should().Contain("\"merged\"");

            var user = await client.GetStringAsync("/user");
            user.Should().Contain("\"two_factor_authentication\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_DynamicRouteAddition_AfterStart()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            simulator.GitHub().Branches().List().UseDefault();

            var response = await client.GetStringAsync("/repos/octocat/hello-world/branches");

            response.Should().StartWith("[");
            response.Should().Contain("\"name\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_Issues_GetAndCreate()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Issues().Get().UseDefault();
        simulator.Issues().Create().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var issue = await client.GetStringAsync("/repos/octocat/hello-world/issues/1");
            issue.Should().Contain("\"state\":\"open\"");

            using var content = new StringContent("{}");
            var createResponse = await client.PostAsync("/repos/octocat/hello-world/issues", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadAsStringAsync();
            created.Should().Contain("\"state\":\"open\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_Search_ShouldReturnWrappedResults()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Search().Repositories().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetStringAsync("/search/repositories?q=test");

            response.Should().Contain("\"total_count\"");
            response.Should().Contain("\"incomplete_results\"");
            response.Should().Contain("\"items\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_WorkflowRuns_ShouldReturnWrappedList()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Actions().ListWorkflowRuns().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetStringAsync("/repos/octocat/hello-world/actions/runs");

            response.Should().Contain("\"total_count\"");
            response.Should().Contain("\"workflow_runs\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_UseAllDefaults_ShouldRegisterAllRoutes()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.UseAllDefaults();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var repo = await client.GetStringAsync("/repos/octocat/hello-world");
            repo.Should().Contain("\"full_name\"");

            var issues = await client.GetStringAsync("/repos/octocat/hello-world/issues");
            issues.Should().StartWith("[");

            var user = await client.GetStringAsync("/users/octocat");
            user.Should().Contain("\"login\"");

            var branches = await client.GetStringAsync("/repos/octocat/hello-world/branches");
            branches.Should().StartWith("[");

            var releases = await client.GetStringAsync("/repos/octocat/hello-world/releases");
            releases.Should().StartWith("[");

            var search = await client.GetStringAsync("/search/repositories?q=test");
            search.Should().Contain("\"total_count\"");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_WithError_ShouldReturnErrorResponse()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Repositories().Get().WithError(404,
            GitHubErrors.NotFound("Not Found"));

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetAsync("/repos/octocat/nonexistent");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Not Found");
            body.Should().Contain("documentation_url");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_WithCustomResponse_ShouldReturnCustomJson()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Repositories().Get().WithResponse("{\"id\":42,\"name\":\"custom-repo\"}");

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetStringAsync("/repos/octocat/custom-repo");

            response.Should().Contain("\"name\":\"custom-repo\"");
            response.Should().Contain("\"id\":42");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public void GitHubMock_UseAllDefaults_IsChainable()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0)
            .UseAllDefaults();

        simulator.Should().NotBeNull();
        simulator.Should().BeOfType<GitHubMock>();
    }

    [Fact]
    public async Task GitHubMock_GitRefs_ShouldMatchSlashDelimitedRefs()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.GitRefs().Get().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            var response = await client.GetStringAsync("/repos/octocat/hello-world/git/ref/heads/main");

            response.Should().Contain("\"ref\"");
            response.Should().Contain("refs/heads/main");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task GitHubMock_DefaultSuccessStatuses_ShouldMatchGitHubEndpoints()
    {
        var simulator = this.CreateGitHubMock(o => o.Port = 0);
        simulator.Repositories().Create().UseDefault();
        simulator.Repositories().Delete().UseDefault();
        simulator.GitRefs().Create().UseDefault();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();

            using var repoContent = new StringContent("{}");
            var createRepositoryResponse = await client.PostAsync("/user/repos", repoContent);
            createRepositoryResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var deleteRepositoryResponse = await client.DeleteAsync("/repos/octocat/hello-world");
            deleteRepositoryResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var refContent = new StringContent("{}");
            var createRefResponse = await client.PostAsync("/repos/octocat/hello-world/git/refs", refContent);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await simulator.StopAsync();
        }
    }
}
