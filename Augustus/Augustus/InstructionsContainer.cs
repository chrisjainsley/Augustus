using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Augustus.Tests")]

namespace Augustus;

/// <summary>
/// Manages instructions for an API simulator instance.
/// </summary>
/// <remarks>
/// This class is instance-based to support running multiple independent simulators concurrently.
/// </remarks>
internal class InstructionsContainer
{
    private readonly string apiName;
    private readonly List<string> defaultInstructions;
    private readonly List<string> globalInstructions = new();
    private readonly List<RouteInstruction> routeInstructions = new();

    /// <summary>
    /// Gets the combined list of default and global instructions.
    /// </summary>
    public List<string> Instructions { get; private set; } = new();

    /// <summary>
    /// Gets the list of route-specific instructions.
    /// </summary>
    public List<RouteInstruction> RouteInstructions => routeInstructions;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstructionsContainer"/> class.
    /// </summary>
    /// <param name="apiName">The name of the API being simulated.</param>
    public InstructionsContainer(string apiName)
    {
        this.apiName = apiName ?? "API";
        defaultInstructions = new List<string>
        {
            $"You are a {this.apiName} API simulator. You only respond to curl commands.",
            "Only output the response body. Always output the full body you would expect from the API.",
            "Generate realistic, properly formatted responses that match the API's expected structure and data types.",
            "Use appropriate HTTP status codes in your responses when applicable."
        };
        UpdateInstructions();
    }

    /// <summary>
    /// Adds a global instruction that applies to all requests.
    /// </summary>
    /// <param name="instruction">The instruction to add.</param>
    public void AddInstruction(string instruction)
    {
        globalInstructions.Add(instruction);
        UpdateInstructions();
    }

    /// <summary>
    /// Adds a route-specific instruction.
    /// </summary>
    /// <param name="routeInstruction">The route instruction to add.</param>
    public void AddRouteInstruction(RouteInstruction routeInstruction)
    {
        routeInstructions.Add(routeInstruction);
    }

    /// <summary>
    /// Clears all global and route-specific instructions.
    /// </summary>
    public void ClearInstructions()
    {
        globalInstructions.Clear();
        routeInstructions.Clear();
        UpdateInstructions();
    }

    /// <summary>
    /// Gets the instructions that apply to a specific request.
    /// </summary>
    /// <param name="path">The request path.</param>
    /// <param name="method">The HTTP method.</param>
    /// <returns>A list of instructions combining default, global, and matching route-specific instructions.</returns>
    public List<string> GetInstructionsForRequest(string path, string method)
    {
        var instructions = new List<string>(defaultInstructions);
        instructions.AddRange(globalInstructions);

        // Find matching route instructions
        var matchingRoute = routeInstructions.FirstOrDefault(r => r.Matches(path, method));
        if (matchingRoute != null)
        {
            instructions.AddRange(matchingRoute.Instructions);
        }

        return instructions;
    }

    private void UpdateInstructions()
    {
        Instructions = new List<string>(defaultInstructions);
        Instructions.AddRange(globalInstructions);
    }
}
