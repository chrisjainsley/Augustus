using Augustus.Reqnroll;
using FluentAssertions;
using Xunit;

namespace Augustus.Reqnroll.Tests;

public class AugustusScenarioHooksTests
{
    [Fact]
    public void ComputeCachePath_ShouldCombineProjectRootFeaturePathAndApiName()
    {
        var result = AugustusScenarioHooks.ComputeCachePath(
            "/projects/myapp", "Features/Charges", "Stripe");

        var expected = Path.Combine("/projects/myapp", "Features/Charges", "__mocks__", "Stripe");
        result.Should().Be(expected);
    }

    [Fact]
    public void ComputeCachePath_ShouldHandleEmptyFeatureFolderPath()
    {
        var result = AugustusScenarioHooks.ComputeCachePath(
            "/projects/myapp", "", "Stripe");

        var expected = Path.Combine("/projects/myapp", "__mocks__", "Stripe");
        result.Should().Be(expected);
    }

    [Fact]
    public void ComputeCachePath_ShouldHandleNullFeatureFolderPath()
    {
        var result = AugustusScenarioHooks.ComputeCachePath(
            "/projects/myapp", null, "PayPal");

        var expected = Path.Combine("/projects/myapp", "__mocks__", "PayPal");
        result.Should().Be(expected);
    }

    [Fact]
    public void ComputeCachePath_ShouldHandleNestedFeaturePaths()
    {
        var result = AugustusScenarioHooks.ComputeCachePath(
            "/projects/myapp", "Features/Billing/Charges", "Stripe");

        var expected = Path.Combine("/projects/myapp", "Features/Billing/Charges", "__mocks__", "Stripe");
        result.Should().Be(expected);
    }
}
