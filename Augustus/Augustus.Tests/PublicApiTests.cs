using PublicApiGenerator;
using VerifyXunit;

namespace Augustus.Tests;

public class PublicApiTests
{
    [Fact]
    public Task ApprovePublicApi()
    {
        var api = typeof(APISimulator).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" }
        });
        return Verifier.Verify(api);
    }
}
