namespace Augustus.Reqnroll;

using System.Diagnostics;
using System.IO;
using global::Reqnroll;

/// <summary>
/// Auto-discovered Reqnroll hooks that manage per-scenario cache paths.
/// Sets cache base path to {projectRoot}/{featureFolderPath}/__mocks__/{apiName}
/// and organizes cache entries into per-scenario subdirectories.
/// </summary>
/// <remarks>
/// These hooks mutate shared APISimulator state. If your test runner enables parallel
/// scenario execution (e.g., xUnit default parallelization), scenarios may race on the
/// same simulator instance. Disable parallel execution in your test assembly or use
/// separate simulator instances per collection to avoid cache path conflicts.
/// </remarks>
[Binding]
public class AugustusScenarioHooks
{
    private readonly ScenarioContext _scenarioContext;
    private readonly FeatureContext _featureContext;

    public AugustusScenarioHooks(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _scenarioContext = scenarioContext;
        _featureContext = featureContext;
    }

    [BeforeScenario(Order = 0)]
    public void BeforeScenario()
    {
        var simulators = AugustusReqnrollContext.Simulators;
        var projectRoot = AugustusReqnrollContext.ProjectRoot;

        if (simulators.Count == 0 || projectRoot == null)
        {
            Debug.WriteLine("Augustus.Reqnroll: No simulators registered. Call AugustusReqnrollContext.Register() in [BeforeTestRun].");
            return;
        }

        var featureFolderPath = _featureContext.FeatureInfo.FolderPath;
        var scenarioTitle = _scenarioContext.ScenarioInfo.Title;

        for (int i = 0; i < simulators.Count; i++)
        {
            var simulator = simulators[i];
            var cachePath = ComputeCachePath(projectRoot, featureFolderPath, simulator.ApiName);
            simulator.SetCacheBasePath(cachePath);
            simulator.SetTestContext(scenarioTitle);

            if (i == 0)
            {
                _scenarioContext.Set(simulator, nameof(APISimulator));
            }
        }
    }

    [AfterScenario(Order = 10000)]
    public void AfterScenario()
    {
        foreach (var simulator in AugustusReqnrollContext.Simulators)
        {
            simulator.ClearTestContext();
        }
    }

    /// <summary>
    /// Computes the cache path: {projectRoot}/{featureFolderPath}/__mocks__/{apiName}
    /// </summary>
    internal static string ComputeCachePath(string projectRoot, string? featureFolderPath, string apiName)
    {
        if (string.IsNullOrEmpty(featureFolderPath))
            return Path.Combine(projectRoot, "__mocks__", apiName);

        return Path.Combine(projectRoot, featureFolderPath, "__mocks__", apiName);
    }
}
