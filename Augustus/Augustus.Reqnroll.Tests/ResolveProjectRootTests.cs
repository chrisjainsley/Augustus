using Augustus.Reqnroll;
using FluentAssertions;
using Xunit;

namespace Augustus.Reqnroll.Tests;

public class ResolveProjectRootTests
{
    [Fact]
    public void ResolveProjectRoot_ShouldFindDirectoryContainingCsproj()
    {
        // Create a temp directory structure with a .csproj file
        var tempRoot = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        var nestedDir = Path.Combine(tempRoot, "Support");
        try
        {
            Directory.CreateDirectory(nestedDir);
            File.WriteAllText(Path.Combine(tempRoot, "Test.csproj"), "<Project />");
            var nestedFile = Path.Combine(nestedDir, "Hooks.cs");
            File.WriteAllText(nestedFile, "// hooks");

            var result = AugustusReqnrollContext.ResolveProjectRoot(nestedFile);

            result.Should().Be(tempRoot);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ResolveProjectRoot_ShouldReturnCallerDirectory_WhenNoCsprojFound()
    {
        // Create a temp directory with no .csproj
        var tempRoot = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempRoot);
            var file = Path.Combine(tempRoot, "SomeFile.cs");
            File.WriteAllText(file, "// code");

            var result = AugustusReqnrollContext.ResolveProjectRoot(file);

            result.Should().Be(tempRoot);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ResolveProjectRoot_ShouldReturnCurrentDirectory_WhenPathIsEmpty()
    {
        var result = AugustusReqnrollContext.ResolveProjectRoot("");

        result.Should().Be(Directory.GetCurrentDirectory());
    }

    [Fact]
    public void ResolveProjectRoot_ShouldWalkUpMultipleLevels()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "augustus_test_" + Guid.NewGuid().ToString("N"));
        var deepDir = Path.Combine(tempRoot, "src", "Features", "Charges");
        try
        {
            Directory.CreateDirectory(deepDir);
            File.WriteAllText(Path.Combine(tempRoot, "MyProject.csproj"), "<Project />");
            var deepFile = Path.Combine(deepDir, "Steps.cs");
            File.WriteAllText(deepFile, "// steps");

            var result = AugustusReqnrollContext.ResolveProjectRoot(deepFile);

            result.Should().Be(tempRoot);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }
}
