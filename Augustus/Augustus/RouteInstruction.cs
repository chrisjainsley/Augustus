using System.Text.RegularExpressions;

namespace Augustus;

/// <summary>
/// Represents a route-specific instruction configuration that applies to matching HTTP requests.
/// </summary>
/// <remarks>
/// Route instructions allow you to provide different AI guidance for different API endpoints and HTTP methods.
/// Patterns support placeholders like {id} for dynamic path segments.
/// </remarks>
public class RouteInstruction
{
    /// <summary>
    /// Gets or sets the URL pattern to match against incoming requests.
    /// </summary>
    /// <value>
    /// A URL pattern that may include placeholders like {id} or {*} for wildcards.
    /// Example: "/api/customers/{id}"
    /// </value>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method to match.
    /// </summary>
    /// <value>
    /// An HTTP method name (GET, POST, PUT, DELETE, PATCH) or "*" to match all methods.
    /// Default is "*". Case-insensitive.
    /// </value>
    public string HttpMethod { get; set; } = "*"; // * means all methods

    /// <summary>
    /// Gets or sets the list of instructions that apply to this route.
    /// </summary>
    /// <value>
    /// A list of instruction strings that guide AI response generation for matching requests.
    /// </value>
    public List<string> Instructions { get; set; } = new();

    private Regex? _compiledPattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteInstruction"/> class.
    /// </summary>
    /// <param name="pattern">The URL pattern to match. Supports {id} for path segments and {*} for wildcards.</param>
    /// <param name="httpMethod">The HTTP method to match, or "*" for all methods. Default is "*".</param>
    /// <exception cref="ArgumentNullException">Thrown if pattern is null.</exception>
    public RouteInstruction(string pattern, string httpMethod = "*")
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        HttpMethod = (httpMethod ?? "*").ToUpperInvariant();
        CompilePattern();
    }

    private void CompilePattern()
    {
        try
        {
            // Convert simple patterns like "/api/customers/{id}" to regex
            var regexPattern = Pattern
                .Replace("{id}", @"[^/]+")
                .Replace("{*}", ".*")
                .Replace("/", @"\/");

            _compiledPattern = new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch
        {
            // If pattern compilation fails, treat as literal match
            _compiledPattern = new Regex($"^{Regex.Escape(Pattern)}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Determines whether this route instruction matches the specified request path and HTTP method.
    /// </summary>
    /// <param name="path">The request path to test (e.g., "/api/customers/123").</param>
    /// <param name="method">The HTTP method to test (e.g., "GET").</param>
    /// <returns><c>true</c> if the path and method match this route's pattern and HTTP method; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Matching is case-insensitive for both the path and HTTP method.
    /// If HttpMethod is "*", any HTTP method will match.
    /// </remarks>
    public bool Matches(string path, string method)
    {
        if (HttpMethod != "*" && !HttpMethod.Equals(method, StringComparison.OrdinalIgnoreCase))
            return false;

        return _compiledPattern?.IsMatch(path) ?? false;
    }
}

/// <summary>
/// A fluent builder for configuring route-specific instructions.
/// </summary>
/// <remarks>
/// The InstructionBuilder provides a chainable API for configuring how the simulator
/// should respond to specific routes and HTTP methods. Build by calling <see cref="Build"/>
/// or using implicit conversion to <see cref="APISimulator"/>.
/// </remarks>
/// <example>
/// <code>
/// simulator.ConfigureRoutes()
///     .ForGet("/api/customers/{id}")
///     .WithInstruction("Return a customer with the specified ID")
///     .WithStatusCode(200)
///     .Build();
/// </code>
/// </example>
public class InstructionBuilder
{
    private readonly APISimulator simulator;
    private readonly List<RouteInstruction> routeInstructions = new();
    private RouteInstruction? currentRoute;

    internal InstructionBuilder(APISimulator simulator)
    {
        this.simulator = simulator;
    }

    /// <summary>
    /// Configures instructions for requests matching the specified route pattern and HTTP method.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers/{id}"). Supports {id} and {*} placeholders.</param>
    /// <param name="httpMethod">The HTTP method to match (GET, POST, PUT, DELETE, PATCH) or "*" for all methods. Default is "*".</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    /// <remarks>
    /// Subsequent calls to <see cref="WithInstruction"/> will add instructions to this route until
    /// another route is configured.
    /// </remarks>
    public InstructionBuilder ForRoute(string pattern, string httpMethod = "*")
    {
        currentRoute = new RouteInstruction(pattern, httpMethod);
        routeInstructions.Add(currentRoute);
        return this;
    }

    /// <summary>
    /// Configures instructions for HTTP GET requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    public InstructionBuilder ForGet(string pattern)
    {
        return ForRoute(pattern, "GET");
    }

    /// <summary>
    /// Configures instructions for HTTP POST requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers").</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    public InstructionBuilder ForPost(string pattern)
    {
        return ForRoute(pattern, "POST");
    }

    /// <summary>
    /// Configures instructions for HTTP PUT requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    public InstructionBuilder ForPut(string pattern)
    {
        return ForRoute(pattern, "PUT");
    }

    /// <summary>
    /// Configures instructions for HTTP DELETE requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    public InstructionBuilder ForDelete(string pattern)
    {
        return ForRoute(pattern, "DELETE");
    }

    /// <summary>
    /// Configures instructions for HTTP PATCH requests matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    public InstructionBuilder ForPatch(string pattern)
    {
        return ForRoute(pattern, "PATCH");
    }

    /// <summary>
    /// Adds an instruction to the currently configured route.
    /// </summary>
    /// <param name="instruction">The instruction to guide AI response generation.</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    /// <remarks>
    /// If no route has been configured (via ForRoute, ForGet, etc.), the instruction
    /// will be added as a global instruction instead.
    /// </remarks>
    public InstructionBuilder WithInstruction(string instruction)
    {
        if (currentRoute != null)
        {
            currentRoute.Instructions.Add(instruction);
        }
        else
        {
            // Add as global instruction if no current route
            simulator.AddInstruction(instruction);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple instructions to the currently configured route.
    /// </summary>
    /// <param name="instructions">The instructions to add.</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    public InstructionBuilder WithInstructions(params string[] instructions)
    {
        foreach (var instruction in instructions)
        {
            WithInstruction(instruction);
        }
        return this;
    }

    /// <summary>
    /// Instructs the AI to return a specific JSON response.
    /// </summary>
    /// <param name="jsonTemplate">The JSON template to return.</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    /// <remarks>
    /// This is a convenience method that adds an instruction to return the specified JSON.
    /// The AI may still modify or enhance the response based on other instructions and request context.
    /// </remarks>
    public InstructionBuilder WithJsonResponse(string jsonTemplate)
    {
        return WithInstruction($"Return the following JSON response: {jsonTemplate}");
    }

    /// <summary>
    /// Instructs the AI to return a specific HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to return (e.g., 200, 404, 500).</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    /// <remarks>
    /// Note: This currently only instructs the AI to include the status code in the response.
    /// The actual HTTP status will be determined by the AI's interpretation.
    /// </remarks>
    public InstructionBuilder WithStatusCode(int statusCode)
    {
        return WithInstruction($"Return HTTP status code {statusCode}");
    }

    /// <summary>
    /// Instructs the AI to simulate a response delay.
    /// </summary>
    /// <param name="milliseconds">The delay in milliseconds.</param>
    /// <returns>This <see cref="InstructionBuilder"/> for method chaining.</returns>
    /// <remarks>
    /// Note: This currently only adds an instruction about delays.
    /// Actual delay implementation is not yet functional.
    /// </remarks>
    public InstructionBuilder WithDelay(int milliseconds)
    {
        return WithInstruction($"Simulate a {milliseconds}ms delay in the response");
    }

    /// <summary>
    /// Finalizes the route configuration and returns the <see cref="APISimulator"/>.
    /// </summary>
    /// <returns>The <see cref="APISimulator"/> that was being configured.</returns>
    /// <remarks>
    /// This method registers all configured route instructions with the simulator.
    /// You can also use implicit conversion instead of explicitly calling Build().
    /// </remarks>
    public APISimulator Build()
    {
        // Register all route instructions with the simulator
        foreach (var routeInstruction in routeInstructions)
        {
            simulator.InstructionsContainer.AddRouteInstruction(routeInstruction);
        }
        return simulator;
    }

    /// <summary>
    /// Implicitly converts the builder to an <see cref="APISimulator"/> by calling <see cref="Build"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <remarks>
    /// This allows you to use the builder directly in contexts expecting an APISimulator
    /// without explicitly calling Build().
    /// </remarks>
    public static implicit operator APISimulator(InstructionBuilder builder)
    {
        return builder.Build();
    }
}