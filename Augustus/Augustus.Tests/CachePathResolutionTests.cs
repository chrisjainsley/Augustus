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

    [Fact]
    public async Task SetTestContext_ShouldCreateSubdirectoryAndRouteCacheOperations()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileManager = new APISimulator.FileManager(tempDir);
            fileManager.SetTestContext("Create a new customer");

            var expectedDir = Path.Combine(tempDir, "Create_a_new_customer");
            Directory.Exists(expectedDir).Should().BeTrue("subdirectory should be created for the test context");

            // Write a cache file and verify it lands in the subdirectory
            await fileManager.CacheResponseAsync("ABCDEF", "{\"test\":true}", "GET /test", new List<string>());
            File.Exists(Path.Combine(expectedDir, "ABCDEF.json")).Should().BeTrue();
            File.Exists(Path.Combine(tempDir, "ABCDEF.json")).Should().BeFalse("file should not be in the root cache folder");

            // Read should find it in the subdirectory
            var cached = await fileManager.ReadCachedResponseAsync("ABCDEF");
            cached.Should().Be("{\"test\":true}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ClearTestContext_ShouldRemoveStaleEntriesForSubdirectoryOnly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileManager = new APISimulator.FileManager(tempDir);

            // Write a file in the root cache folder
            await File.WriteAllTextAsync(Path.Combine(tempDir, "ROOT123.json"), "{}");

            // Set context and write a file, then write a stale file
            fileManager.SetTestContext("My Scenario");
            await fileManager.CacheResponseAsync("TOUCHED", "{}", "GET /t", new List<string>());
            var contextDir = Path.Combine(tempDir, "My_Scenario");
            await File.WriteAllTextAsync(Path.Combine(contextDir, "STALE99.json"), "{}");

            // Clear context - should remove STALE99 but keep TOUCHED and ROOT123
            fileManager.ClearTestContext();

            File.Exists(Path.Combine(contextDir, "TOUCHED.json")).Should().BeTrue("touched file in context should be kept");
            File.Exists(Path.Combine(contextDir, "STALE99.json")).Should().BeFalse("stale file in context should be removed");
            File.Exists(Path.Combine(tempDir, "ROOT123.json")).Should().BeTrue("root cache file should not be affected");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task NoTestContext_ShouldUseFlatCacheBehavior()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileManager = new APISimulator.FileManager(tempDir);

            // Without SetTestContext, cache operations should go to the root
            await fileManager.CacheResponseAsync("FLAT01", "{\"flat\":true}", "GET /flat", new List<string>());
            File.Exists(Path.Combine(tempDir, "FLAT01.json")).Should().BeTrue("file should be in the root cache folder");

            var cached = await fileManager.ReadCachedResponseAsync("FLAT01");
            cached.Should().Be("{\"flat\":true}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SetCacheBasePath_ShouldChangeBasePathAndWriteThere()
    {
        var originalDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        var newBaseDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileManager = new APISimulator.FileManager(originalDir);

            fileManager.SetCacheBasePath(newBaseDir);

            await fileManager.CacheResponseAsync("NEWBASE1", "{\"moved\":true}", "GET /test", new List<string>());
            File.Exists(Path.Combine(newBaseDir, "NEWBASE1.json")).Should().BeTrue("file should be in the new base path");
            File.Exists(Path.Combine(originalDir, "NEWBASE1.json")).Should().BeFalse("file should not be in the original path");
        }
        finally
        {
            if (Directory.Exists(originalDir)) Directory.Delete(originalDir, true);
            if (Directory.Exists(newBaseDir)) Directory.Delete(newBaseDir, true);
        }
    }

    [Fact]
    public async Task SetCacheBasePath_WithTestContext_ShouldRouteToNewBaseSubdirectory()
    {
        var originalDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        var newBaseDir = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var fileManager = new APISimulator.FileManager(originalDir);

            fileManager.SetCacheBasePath(newBaseDir);
            fileManager.SetTestContext("My Scenario");

            var expectedDir = Path.Combine(newBaseDir, "My_Scenario");
            await fileManager.CacheResponseAsync("COMBO1", "{\"combo\":true}", "GET /test", new List<string>());
            File.Exists(Path.Combine(expectedDir, "COMBO1.json")).Should().BeTrue("file should be in new base + scenario subdirectory");
            File.Exists(Path.Combine(originalDir, "COMBO1.json")).Should().BeFalse();
            File.Exists(Path.Combine(newBaseDir, "COMBO1.json")).Should().BeFalse("file should be in subdirectory, not base");
        }
        finally
        {
            if (Directory.Exists(originalDir)) Directory.Delete(originalDir, true);
            if (Directory.Exists(newBaseDir)) Directory.Delete(newBaseDir, true);
        }
    }

    [Theory]
    [InlineData("Simple Name", "Simple_Name")]
    [InlineData("Has:colons:here", "Has_colons_here")]
    [InlineData("Slashes/and\\backslashes", "Slashes_and_backslashes")]
    [InlineData("Multiple   spaces", "Multiple_spaces")]
    [InlineData("Special<>chars\"test", "Special_chars_test")]
    [InlineData("Trailing space ", "Trailing_space")]
    public void SanitizeFolderName_ShouldHandleSpecialCharacters(string input, string expected)
    {
        APISimulator.FileManager.SanitizeFolderName(input).Should().Be(expected);
    }
}
