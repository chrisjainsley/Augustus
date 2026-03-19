using FluentAssertions;

namespace Augustus.Tests;

public class CacheOnlyModeTests
{
    [Fact]
    public void CacheOnly_ShouldForceAutoRemoveStaleCacheFalse()
    {
        var options = new APISimulatorOptions
        {
            AutoRemoveStaleCache = true,
            CacheOnly = true
        };

        options.AutoRemoveStaleCache.Should().BeFalse();
    }

    [Fact]
    public void CacheOnly_ShouldForceEnableCachingTrue()
    {
        var options = new APISimulatorOptions
        {
            EnableCaching = false,
            CacheOnly = true
        };

        options.EnableCaching.Should().BeTrue();
    }
}
