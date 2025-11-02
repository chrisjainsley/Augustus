namespace Augustus.Extensions;

/// <summary>
/// Extension methods for integrating APISimulator with test frameworks.
/// </summary>
public static class TestFrameworkExtensions
{
    /// <summary>
    /// Creates a new API simulator instance.
    /// </summary>
    /// <param name="testClass">The test class instance (typically 'this').</param>
    /// <param name="configureOptions">Optional action to configure server options.</param>
    /// <returns>A new <see cref="APISimulator"/> instance.</returns>
    /// <remarks>
    /// This extension method works with any test framework (xUnit, NUnit, MSTest, etc.).
    /// The testClass parameter enables the fluent API pattern: this.CreateAPISimulator().
    /// </remarks>
    /// <example>
    /// <code>
    /// var simulator = this.CreateAPISimulator(opts => opts.Port = 8080)
    ///     .ForGet("/api/test")
    ///     .WithResponse(new { message = "Hello" })
    ///     .Add();
    /// await simulator.StartAsync();
    /// </code>
    /// </example>
    public static APISimulator CreateAPISimulator(this object testClass, Action<APISimulatorOptions>? configureOptions = null)
    {
        var options = new APISimulatorOptions();
        configureOptions?.Invoke(options);
        return new APISimulator(options);
    }
}
