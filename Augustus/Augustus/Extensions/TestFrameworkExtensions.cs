namespace Augustus.Extensions;

/// <summary>
/// Extension methods for integrating MockServer with test frameworks.
/// </summary>
public static class TestFrameworkExtensions
{
    /// <summary>
    /// Creates a new mock HTTP server instance.
    /// </summary>
    /// <param name="testClass">The test class instance (typically 'this').</param>
    /// <param name="configureOptions">Optional action to configure server options.</param>
    /// <returns>A new <see cref="MockServer"/> instance.</returns>
    /// <remarks>
    /// This extension method works with any test framework (xUnit, NUnit, MSTest, etc.).
    /// The testClass parameter enables the fluent API pattern: this.CreateMockServer().
    /// </remarks>
    /// <example>
    /// <code>
    /// var mock = this.CreateMockServer(opts => opts.Port = 8080)
    ///     .ForGet("/api/test")
    ///     .WithResponse(new { message = "Hello" })
    ///     .Add();
    /// await mock.StartAsync();
    /// </code>
    /// </example>
    public static MockServer CreateMockServer(this object testClass, Action<MockServerOptions>? configureOptions = null)
    {
        var options = new MockServerOptions();
        configureOptions?.Invoke(options);
        return new MockServer(options);
    }
}
