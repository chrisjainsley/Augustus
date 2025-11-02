namespace Augustus;

using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// A flexible HTTP API simulator for testing.
/// </summary>
/// <remarks>
/// APISimulator allows you to create mock HTTP endpoints with customizable responses.
/// Routes can be configured before starting the server or added dynamically after it's running.
/// </remarks>
public class APISimulator : IAsyncDisposable
{
    private readonly APISimulatorOptions options;
    private readonly List<RouteConfiguration> routes = new();
    private readonly object routesLock = new();
    private WebHost? webHost;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="APISimulator"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the API simulator.</param>
    public APISimulator(APISimulatorOptions? options = null)
    {
        this.options = options ?? new APISimulatorOptions();
    }

    /// <summary>
    /// Gets a value indicating whether the API simulator is currently running.
    /// </summary>
    public bool IsRunning => webHost?.IsRunning ?? false;

    /// <summary>
    /// Starts the API simulator asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (webHost != null)
            throw new InvalidOperationException("APISimulator is already started. Call StopAsync() before starting again.");

        webHost = new WebHost(options, this);
        await webHost.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the API simulator asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (webHost == null)
            return;

        await webHost.StopAsync(cancellationToken);
        await webHost.DisposeAsync();
        webHost = null;
    }

    /// <summary>
    /// Creates an HttpClient configured to communicate with the API simulator.
    /// </summary>
    /// <returns>A new HttpClient instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the simulator is not started.</exception>
    public HttpClient CreateClient()
    {
        if (webHost == null)
            throw new InvalidOperationException("APISimulator must be started before creating clients. Call StartAsync() first.");

        return webHost.CreateClient();
    }

    #region Fluent API for Route Configuration

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

    #endregion

    #region Route Management

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

    #endregion

    /// <summary>
    /// Asynchronously disposes of the API simulator and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        try
        {
            await StopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during APISimulator disposal: {ex}");
        }

        disposed = true;
        GC.SuppressFinalize(this);
    }
}
