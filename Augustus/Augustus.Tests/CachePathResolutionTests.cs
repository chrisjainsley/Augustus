using FluentAssertions;

namespace Augustus.Tests;

public class CachePathResolutionTests
{
    [Fact]
    public void ResolveCachePath_ShouldDeriveFromCallerFilePathAndApiName()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "src", "Tests");
        var callerFilePath = Path.Combine(testDir, "StripeTests.cs");
        var result = APISimulatorOptions.ResolveCacheFolderPath(callerFilePath, "Stripe", "./mocks");

        var expected = Path.Combine(testDir, "__mocks__", "StripeTests", "Stripe");
        result.Should().Be(expected);
    }

    [Fact]
    public void ResolveCachePath_ShouldReturnExplicitPath_WhenCacheFolderPathIsSet()
    {
        var options = new APISimulatorOptions();
        options.CacheFolderPath = "./custom-mocks";

        options.IsCacheFolderPathExplicitlySet.Should().BeTrue();
        options.CacheFolderPath.Should().Be("./custom-mocks");
    }

    [Fact]
    public void ResolveCachePath_ShouldFallbackToDefault_WhenNoCallerFilePath()
    {
        var result = APISimulatorOptions.ResolveCacheFolderPath("", "Stripe", "./mocks");
        result.Should().Be("./mocks");

        var resultNull = APISimulatorOptions.ResolveCacheFolderPath(null, "Stripe", "./mocks");
        resultNull.Should().Be("./mocks");
    }

    [Fact]
    public void ResolveCachePath_ShouldHandleNestedPaths()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "Projects", "MyTests");
        var callerFilePath = Path.Combine(testDir, "PaymentTests.cs");
        var result = APISimulatorOptions.ResolveCacheFolderPath(callerFilePath, "PayPal", "./mocks");

        var expected = Path.Combine(testDir, "__mocks__", "PaymentTests", "PayPal");
        result.Should().Be(expected);
    }
}
