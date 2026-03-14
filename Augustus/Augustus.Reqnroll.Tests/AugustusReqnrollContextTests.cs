using Augustus.Reqnroll;
using FluentAssertions;
using Xunit;

namespace Augustus.Reqnroll.Tests;

public class AugustusReqnrollContextTests : IDisposable
{
    public AugustusReqnrollContextTests()
    {
        // Ensure clean state before each test
        AugustusReqnrollContext.Clear();
    }

    public void Dispose()
    {
        AugustusReqnrollContext.Clear();
    }

    [Fact]
    public void Register_ShouldStoreSimulatorAndCaptureProjectRoot()
    {
        var simulator = CreateTestSimulator("TestApi");

        AugustusReqnrollContext.Register(simulator);

        AugustusReqnrollContext.Simulators.Should().ContainSingle()
            .Which.Should().BeSameAs(simulator);
        AugustusReqnrollContext.ProjectRoot.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Register_ShouldSupportMultipleSimulators()
    {
        var sim1 = CreateTestSimulator("Stripe");
        var sim2 = CreateTestSimulator("PayPal");

        AugustusReqnrollContext.Register(sim1);
        AugustusReqnrollContext.Register(sim2);

        AugustusReqnrollContext.Simulators.Should().HaveCount(2);
        AugustusReqnrollContext.Simulators[0].ApiName.Should().Be("Stripe");
        AugustusReqnrollContext.Simulators[1].ApiName.Should().Be("PayPal");
    }

    [Fact]
    public void Clear_ShouldRemoveAllRegistrationsAndResetProjectRoot()
    {
        AugustusReqnrollContext.Register(CreateTestSimulator("Api"));

        AugustusReqnrollContext.Clear();

        AugustusReqnrollContext.Simulators.Should().BeEmpty();
        AugustusReqnrollContext.ProjectRoot.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldThrowOnNullSimulator()
    {
        var act = () => AugustusReqnrollContext.Register(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static APISimulator CreateTestSimulator(string apiName)
    {
        return new APISimulator(apiName, new APISimulatorOptions
        {
            OpenAIApiKey = "test-key",
            EnableCaching = false
        });
    }
}
