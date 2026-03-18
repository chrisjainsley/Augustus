using FluentAssertions;

namespace Augustus.Tests;

public class CacheOnlyModeTests
{
    [Fact]
    public void CacheOnly_ShouldForceAutoRemoveStaleCacheFalse()
    {
        var options = new APISimulatorOptions
        {
            CacheOnly = true,
            AutoRemoveStaleCache = true
        };

        options.Validate();

        options.AutoRemoveStaleCache.Should().BeFalse();
    }

    [Fact]
    public void CacheOnly_ShouldForceEnableCachingTrue()
    {
        var options = new APISimulatorOptions
        {
            CacheOnly = true,
            EnableCaching = false
        };

        options.Validate();

        options.EnableCaching.Should().BeTrue();
    }
}
