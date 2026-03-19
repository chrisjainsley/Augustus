namespace Augustus.Extensions;

using System.Runtime.CompilerServices;

/// <summary>
/// Extension methods for test frameworks to simplify <see cref="APISimulator"/> usage.
/// </summary>
/// <remarks>
/// These extensions provide a fluent API for creating and configuring API simulators
/// within test methods. Works with xUnit, NUnit, MSTest, and other testing frameworks.
/// For AI-powered simulators, see <c>AITestFrameworkExtensions</c> in the Augustus.AI package.
/// </remarks>
public static class TestFrameworkExtensions
{
    /// <summary>
    /// Creates a new API simulator with the specified name and configuration.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <param name="callerFilePath">Auto-populated by the compiler to resolve cache paths.</param>
    /// <returns>A new <see cref="APISimulator"/> instance.</returns>
    public static APISimulator CreateAPISimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("API", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new API simulator with the specified API name and configuration.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="apiName">The name of the API to simulate (e.g., "Stripe", "PayPal").</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <param name="callerFilePath">Auto-populated by the compiler to resolve cache paths.</param>
    /// <returns>A new <see cref="APISimulator"/> instance.</returns>
    public static APISimulator CreateAPISimulator(this object testClass, string apiName, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var options = new APISimulatorOptions();
        configure?.Invoke(options);
        options.TestClassFilePath = callerFilePath;

        return new APISimulator(apiName, options);
    }

    /// <summary>
    /// Creates a new Stripe API simulator with pre-configured context.
    /// </summary>
    public static APISimulator CreateStripeSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("Stripe", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new PayPal API simulator with pre-configured context.
    /// </summary>
    public static APISimulator CreatePayPalSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("PayPal", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new Twilio API simulator with pre-configured context.
    /// </summary>
    public static APISimulator CreateTwilioSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("Twilio", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new Slack API simulator with pre-configured context.
    /// </summary>
    public static APISimulator CreateSlackSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("Slack", configure, callerFilePath);
    }

    /// <summary>
    /// Starts the simulator asynchronously and returns it for method chaining.
    /// </summary>
    public static async Task<APISimulator> StartSimulatorAsync(this APISimulator simulator, CancellationToken cancellationToken = default)
    {
        await simulator.StartAsync(cancellationToken).ConfigureAwait(false);
        return simulator;
    }

    /// <summary>
    /// Adds a global instruction to the simulator and returns it for method chaining.
    /// </summary>
    public static APISimulator WithInstruction(this APISimulator simulator, string instruction)
    {
        simulator.AddInstruction(instruction);
        return simulator;
    }

    /// <summary>
    /// Adds multiple global instructions to the simulator and returns it for method chaining.
    /// </summary>
    public static APISimulator WithInstructions(this APISimulator simulator, params string[] instructions)
    {
        foreach (var instruction in instructions)
        {
            simulator.AddInstruction(instruction);
        }
        return simulator;
    }

    /// <summary>
    /// Creates a builder for configuring route-specific instructions.
    /// </summary>
    public static InstructionBuilder ConfigureRoutes(this APISimulator simulator)
    {
        return simulator.ConfigureRoutes();
    }

    /// <summary>
    /// Configures route-specific instructions for a given URL pattern and HTTP method.
    /// </summary>
    public static InstructionBuilder ForRoute(this APISimulator simulator, string pattern, string httpMethod = "*")
    {
        return simulator.ConfigureRoutes().ForRoute(pattern, httpMethod);
    }

    /// <summary>
    /// Configures route-specific instructions for a given URL pattern and HTTP method.
    /// </summary>
    public static InstructionBuilder ForRoute(this APISimulator simulator, string pattern, HttpVerb httpVerb)
    {
        return simulator.ConfigureRoutes().ForRoute(pattern, httpVerb);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP GET requests matching the pattern.
    /// </summary>
    public static InstructionBuilder ForGet(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForGet(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP POST requests matching the pattern.
    /// </summary>
    public static InstructionBuilder ForPost(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForPost(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP PUT requests matching the pattern.
    /// </summary>
    public static InstructionBuilder ForPut(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForPut(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP DELETE requests matching the pattern.
    /// </summary>
    public static InstructionBuilder ForDelete(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForDelete(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP PATCH requests matching the pattern.
    /// </summary>
    public static InstructionBuilder ForPatch(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForPatch(pattern);
    }
}
