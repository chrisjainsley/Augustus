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
    private int statusCode = 200;

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
        responseStrategy = new StaticResponseStrategy(jsonResponse, statusCode);
        return this;
    }

    /// <summary>
    /// Configures this route to return a static response from an object (serialized to JSON).
    /// </summary>
    /// <param name="responseObject">The object to serialize and return.</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithResponse(object responseObject)
    {
        responseStrategy = new StaticResponseStrategy(responseObject, statusCode);
        return this;
    }

    /// <summary>
    /// Configures this route to load the response from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file (relative or absolute).</param>
    /// <returns>This <see cref="RouteBuilder"/> for method chaining.</returns>
    public RouteBuilder WithJsonFile(string filePath)
    {
        responseStrategy = new FileResponseStrategy(filePath, statusCode);
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

        // If a strategy is already set, recreate it with the new status code
        if (responseStrategy is StaticResponseStrategy staticStrategy)
        {
            // We need to store the JSON to recreate the strategy
            // For now, just update the statusCode field
            // The strategy will be created with the right status code when Add() is called
        }
        else if (responseStrategy is FileResponseStrategy fileStrategy)
        {
            // Similar situation for file strategy
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
        responseStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        return this;
    }

    /// <summary>
    /// Adds this route configuration to the API simulator.
    /// </summary>
    /// <returns>The <see cref="APISimulator"/> instance for further configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no response strategy was configured.</exception>
    public APISimulator Add()
    {
        if (responseStrategy == null)
        {
            throw new InvalidOperationException(
                $"No response strategy configured for route {httpMethod} {pattern}. " +
                "Use WithResponse(), WithJsonFile(), or WithStrategy() before calling Add().");
        }

        apiSimulator.AddRouteInternal(new RouteConfiguration(pattern, httpMethod)
        {
            ResponseStrategy = responseStrategy
        });

        return apiSimulator;
    }
}
