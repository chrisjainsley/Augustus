namespace Augustus;

/// <summary>
/// Route management extensions for the API simulator - supports RouteBuilder-based route configuration.
/// </summary>
public partial class APISimulator
{
    private readonly List<RouteConfiguration> routes = new();
    private readonly object routesLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="APISimulator"/> class with default API name.
    /// </summary>
    /// <param name="options">Configuration options for the simulator.</param>
    public APISimulator(APISimulatorOptions options) : this("API", options)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the API simulator is currently running.
    /// </summary>
    public bool IsRunning => webHost != null;

    /// <summary>
    /// Configures a route for GET requests.
    /// </summary>
    /// <param name="pattern">The URL pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>A <see cref="RouteBuilder"/> for configuring the route.</returns>
    public RouteBuilder ForGet(string pattern)
    {
        return new RouteBuilder(this, pattern, "GET");
    }

    /// <summary>
    /// Configures a route for POST requests.
    /// </summary>
    /// <param name="pattern">The URL pattern to match.</param>
    /// <returns>A <see cref="RouteBuilder"/> for configuring the route.</returns>
    public RouteBuilder ForPost(string pattern)
    {
        return new RouteBuilder(this, pattern, "POST");
    }

    /// <summary>
    /// Configures a route for PUT requests.
    /// </summary>
    /// <param name="pattern">The URL pattern to match.</param>
    /// <returns>A <see cref="RouteBuilder"/> for configuring the route.</returns>
    public RouteBuilder ForPut(string pattern)
    {
        return new RouteBuilder(this, pattern, "PUT");
    }

    /// <summary>
    /// Configures a route for DELETE requests.
    /// </summary>
    /// <param name="pattern">The URL pattern to match.</param>
    /// <returns>A <see cref="RouteBuilder"/> for configuring the route.</returns>
    public RouteBuilder ForDelete(string pattern)
    {
        return new RouteBuilder(this, pattern, "DELETE");
    }

    /// <summary>
    /// Configures a route for PATCH requests.
    /// </summary>
    /// <param name="pattern">The URL pattern to match.</param>
    /// <returns>A <see cref="RouteBuilder"/> for configuring the route.</returns>
    public RouteBuilder ForPatch(string pattern)
    {
        return new RouteBuilder(this, pattern, "PATCH");
    }

    /// <summary>
    /// Configures a route for any HTTP method.
    /// </summary>
    /// <param name="pattern">The URL pattern to match.</param>
    /// <param name="httpMethod">The HTTP method to match, or "*" for all methods.</param>
    /// <returns>A <see cref="RouteBuilder"/> for configuring the route.</returns>
    public RouteBuilder ForRoute(string pattern, string httpMethod = "*")
    {
        return new RouteBuilder(this, pattern, httpMethod);
    }

    /// <summary>
    /// Adds a route configuration to the server (internal use by RouteBuilder).
    /// </summary>
    internal void AddRouteInternal(RouteConfiguration route)
    {
        lock (routesLock)
        {
            routes.Add(route);
        }
    }

    /// <summary>
    /// Removes a route from the server.
    /// </summary>
    /// <param name="pattern">The URL pattern of the route to remove.</param>
    /// <param name="httpMethod">The HTTP method of the route to remove (or "*" for all methods).</param>
    /// <returns>True if the route was found and removed; otherwise, false.</returns>
    public bool RemoveRoute(string pattern, string httpMethod = "*")
    {
        lock (routesLock)
        {
            var route = routes.FirstOrDefault(r =>
                r.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase) &&
                r.HttpMethod.Equals(httpMethod, StringComparison.OrdinalIgnoreCase));

            if (route != null)
            {
                routes.Remove(route);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Clears all routes from the server.
    /// </summary>
    public void ClearRoutes()
    {
        lock (routesLock)
        {
            routes.Clear();
        }
    }

    /// <summary>
    /// Gets the route configuration for a specific request.
    /// </summary>
    internal RouteConfiguration? GetRouteForRequest(string path, string method)
    {
        lock (routesLock)
        {
            return routes.FirstOrDefault(r => r.Matches(path, method));
        }
    }
}
