namespace Augustus;

/// <summary>
/// A fluent builder for configuring API simulator routes.
/// </summary>
public class RouteBuilder
{
    private readonly APISimulator apiSimulator;
    private readonly string pattern;
    private readonly string httpMethod;
    private IResponseStrategy? responseStrategy;
    private Func<int, IResponseStrategy>? strategyFactory;
    private int statusCode = 200;
    private readonly List<string> instructions = new();

    internal RouteBuilder(APISimulator apiSimulator, string pattern, string httpMethod)
    {
        this.apiSimulator = apiSimulator ?? throw new ArgumentNullException(nameof(apiSimulator));
        this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this.httpMethod = httpMethod;
    }

    /// <summary>
    /// Configures this route to return a static JSON response.
    /// </summary>
    /// <param name="jsonResponse">The JSON string to return.</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithResponse(string jsonResponse)
    {
        strategyFactory = code => new StaticResponseStrategy(jsonResponse, code);
        responseStrategy = strategyFactory(statusCode);
        return this;
    }

    /// <summary>
    /// Configures this route to return a static response from an object (serialized to JSON).
    /// </summary>
    /// <param name="responseObject">The object to serialize and return.</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithResponse(object responseObject)
    {
        strategyFactory = code => new StaticResponseStrategy(responseObject, code);
        responseStrategy = strategyFactory(statusCode);
        return this;
    }

    /// <summary>
    /// Configures this route to load the response from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file (relative or absolute).</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithJsonFile(string filePath)
    {
        strategyFactory = code => new FileResponseStrategy(filePath, code);
        responseStrategy = strategyFactory(statusCode);
        return this;
    }

    /// <summary>
    /// Sets the HTTP status code for this route's response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (e.g., 200, 404, 500).</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithStatusCode(int statusCode)
    {
        this.statusCode = statusCode;

        if (strategyFactory != null)
        {
            responseStrategy = strategyFactory(statusCode);
        }

        return this;
    }

    /// <summary>
    /// Sets a custom response strategy for this route.
    /// </summary>
    /// <param name="strategy">The response strategy to use.</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithStrategy(IResponseStrategy strategy)
    {
        strategyFactory = null;
        responseStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        return this;
    }

    /// <summary>
    /// Adds an instruction for AI-powered responses on this route.
    /// </summary>
    /// <param name="instruction">The instruction to add.</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithInstruction(string instruction)
    {
        instructions.Add(instruction);
        return this;
    }

    /// <summary>
    /// Configures this route to return a static JSON response (alias for WithResponse).
    /// </summary>
    /// <param name="jsonResponse">The JSON string to return.</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithJsonResponse(string jsonResponse)
    {
        return WithResponse(jsonResponse);
    }

    /// <summary>
    /// Adds this route configuration to the API simulator.
    /// </summary>
    /// <returns>The <see cref="APISimulator"/> instance for further configuration.</returns>
    public APISimulator Add()
    {
        // Only require response strategy if we're finalizing this route
        if (responseStrategy != null)
        {
            apiSimulator.AddRouteInternal(new RouteConfiguration(pattern, httpMethod)
            {
                ResponseStrategy = responseStrategy
            });
        }

        // Register instructions with the instructions container if any exist
        if (instructions.Count > 0)
        {
            var routeInstruction = new RouteInstruction(pattern, httpMethod);
            foreach (var instruction in instructions)
            {
                routeInstruction.Instructions.Add(instruction);
            }
            apiSimulator.InstructionsContainer.AddRouteInstruction(routeInstruction);
        }

        return apiSimulator;
    }

    /// <summary>
    /// Registers this route's instructions and configuration, then starts a new route for GET requests.
    /// </summary>
    /// <param name="newPattern">The URL pattern for the new GET route.</param>
    /// <returns>A new <see cref="RouteBuilder"/> for the GET route.</returns>
    public RouteBuilder ForGet(string newPattern)
    {
        Add();
        return new RouteBuilder(apiSimulator, newPattern, "GET");
    }

    /// <summary>
    /// Registers this route's instructions and configuration, then starts a new route for POST requests.
    /// </summary>
    /// <param name="newPattern">The URL pattern for the new POST route.</param>
    /// <returns>A new <see cref="RouteBuilder"/> for the POST route.</returns>
    public RouteBuilder ForPost(string newPattern)
    {
        Add();
        return new RouteBuilder(apiSimulator, newPattern, "POST");
    }

    /// <summary>
    /// Registers this route's instructions and configuration, then starts a new route for PUT requests.
    /// </summary>
    /// <param name="newPattern">The URL pattern for the new PUT route.</param>
    /// <returns>A new <see cref="RouteBuilder"/> for the PUT route.</returns>
    public RouteBuilder ForPut(string newPattern)
    {
        Add();
        return new RouteBuilder(apiSimulator, newPattern, "PUT");
    }

    /// <summary>
    /// Registers this route's instructions and configuration, then starts a new route for DELETE requests.
    /// </summary>
    /// <param name="newPattern">The URL pattern for the new DELETE route.</param>
    /// <returns>A new <see cref="RouteBuilder"/> for the DELETE route.</returns>
    public RouteBuilder ForDelete(string newPattern)
    {
        Add();
        return new RouteBuilder(apiSimulator, newPattern, "DELETE");
    }

    /// <summary>
    /// Registers this route's instructions and configuration, then starts a new route for PATCH requests.
    /// </summary>
    /// <param name="newPattern">The URL pattern for the new PATCH route.</param>
    /// <returns>A new <see cref="RouteBuilder"/> for the PATCH route.</returns>
    public RouteBuilder ForPatch(string newPattern)
    {
        Add();
        return new RouteBuilder(apiSimulator, newPattern, "PATCH");
    }

    /// <summary>
    /// Finalizes the route configuration and returns the API simulator.
    /// </summary>
    /// <returns>The <see cref="APISimulator"/> instance.</returns>
    public APISimulator Build()
    {
        return Add();
    }
}
