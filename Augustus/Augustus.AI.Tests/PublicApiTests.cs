using Augustus.AI;
using PublicApiGenerator;
using VerifyXunit;

namespace Augustus.AI.Tests;

public class PublicApiTests
{
    [Fact]
    public Task ApprovePublicApi()
    {
        var api = typeof(AIOptions).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" }
        });
        return Verifier.Verify(api);
    }
}
