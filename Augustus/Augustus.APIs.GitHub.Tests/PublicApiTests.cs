using Augustus.APIs.GitHub;
using PublicApiGenerator;
using VerifyXunit;

namespace Augustus.GitHub.Tests;

public class PublicApiTests
{
    [Fact]
    public Task ApprovePublicApi()
    {
        var api = typeof(GitHubMock).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" }
        });
        return Verifier.Verify(api);
    }
}
