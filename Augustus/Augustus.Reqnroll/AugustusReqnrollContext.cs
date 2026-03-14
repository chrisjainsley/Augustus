namespace Augustus.Reqnroll;

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// Static registration point for Augustus simulators in Reqnroll projects.
/// Call <see cref="Register"/> in a [BeforeTestRun] hook to register simulators.
/// The auto-discovered <see cref="AugustusScenarioHooks"/> will handle per-scenario
/// cache path routing using FeatureInfo.FolderPath.
/// </summary>
public static class AugustusReqnrollContext
{
    private static readonly List<APISimulator> _simulators = new();
    private static string? _projectRoot;

    /// <summary>
    /// Gets the registered simulators.
    /// </summary>
    internal static IReadOnlyList<APISimulator> Simulators => _simulators;

    /// <summary>
    /// Gets the resolved project root directory.
    /// </summary>
    internal static string? ProjectRoot => _projectRoot;

    /// <summary>
    /// Registers an API simulator for automatic per-scenario cache management.
    /// The project root is resolved by walking up from the caller's source file
    /// to find the directory containing a .csproj file.
    /// </summary>
    /// <param name="simulator">The API simulator to register.</param>
    /// <param name="callerFilePath">Automatically captured caller file path. Do not pass explicitly.</param>
    public static void Register(APISimulator simulator, [CallerFilePath] string callerFilePath = "")
    {
        _simulators.Add(simulator ?? throw new ArgumentNullException(nameof(simulator)));

        if (_projectRoot == null)
        {
            _projectRoot = ResolveProjectRoot(callerFilePath);
        }
    }

    /// <summary>
    /// Clears all registrations and resets the project root. Call this in [AfterTestRun] if needed.
    /// </summary>
    public static void Clear()
    {
        _simulators.Clear();
        _projectRoot = null;
    }

    /// <summary>
    /// Walks up the directory tree from a source file path to find the directory
    /// containing a .csproj file. Returns the caller's directory as a fallback.
    /// </summary>
    internal static string ResolveProjectRoot(string callerFilePath)
    {
        if (string.IsNullOrEmpty(callerFilePath))
            return Directory.GetCurrentDirectory();

        var directory = Path.GetDirectoryName(callerFilePath);
        if (string.IsNullOrEmpty(directory))
            return Directory.GetCurrentDirectory();

        var fallback = directory;
        var current = directory;

        while (!string.IsNullOrEmpty(current))
        {
            var csprojFiles = Directory.GetFiles(current, "*.csproj");
            if (csprojFiles.Length > 0)
                return current;

            var parent = Directory.GetParent(current);
            if (parent == null)
                break;

            current = parent.FullName;
        }

        return fallback;
    }
}
