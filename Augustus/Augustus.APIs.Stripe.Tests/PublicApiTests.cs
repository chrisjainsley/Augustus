using Augustus.APIs.Stripe;
using PublicApiGenerator;
using VerifyXunit;

namespace Augustus.Stripe.Tests;

public class PublicApiTests
{
    [Fact]
    public Task ApprovePublicApi()
    {
        var api = typeof(StripeMock).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" }
        });
        return Verifier.Verify(api);
    }
}
