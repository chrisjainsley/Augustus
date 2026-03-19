namespace Augustus;

using System;
using System.Threading.Tasks;

/// <summary>
/// An HTTP API simulator that serves configured route responses with optional AI-powered default handler.
/// </summary>
/// <remarks>
/// The API simulator creates a local web server that intercepts HTTP requests and dispatches them
/// to configured route strategies (static JSON, file-based, or custom). When the Augustus.AI package
/// is installed, an AI-powered default handler can generate realistic responses for unmatched routes.
/// Implements <see cref="IAsyncDisposable"/> for proper resource cleanup.
/// </remarks>
/// <example>
/// <code>
/// var options = new APISimulatorOptions { Port = 9001 };
/// await using var simulator = new APISimulator("Stripe", options);
/// simulator.ForGet("/v1/customers/{id}").WithJsonFile("./mocks/customer.json").Add();
/// await simulator.StartAsync();
/// var client = simulator.CreateClient();
/// // Make requests to the client...
/// // DisposeAsync will be called automatically
/// </code>
/// </example>
public sealed partial class APISimulator : IAsyncDisposable
{
    private readonly string apiName;
    private readonly APISimulatorOptions options;
    private readonly WebHost webHost = new();
    private readonly InstructionsContainer instructionsContainer;
    private readonly FileManager fileManager;
    private readonly RoutingRequestHandler routingHandler;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="APISimulator"/> class.
    /// </summary>
    /// <param name="apiName">The name of the API being simulated (e.g., "Stripe", "PayPal"). Used for context in responses.</param>
    /// <param name="options">Configuration options for the simulator, including port and caching settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiName"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">Thrown when options validation fails.</exception>
    public APISimulator(string apiName, APISimulatorOptions options)
    {
        this.apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        // Validate options early (fail-fast) instead of waiting until first request
        options.Validate();

        // Resolve per-test-class cache path if not explicitly set
        if (!options.IsCacheFolderPathExplicitlySet && !string.IsNullOrEmpty(options.TestClassFilePath))
        {
            options.CacheFolderPath = APISimulatorOptions.ResolveCacheFolderPath(
                options.TestClassFilePath, apiName, options.CacheFolderPath);
            // Reset the explicit flag since this was auto-resolved, not user-set
            options.ResetCacheFolderPathExplicitFlag();
        }

        fileManager = new FileManager(options.CacheFolderPath);
        instructionsContainer = new InstructionsContainer(apiName);

        routingHandler = new RoutingRequestHandler(this);
        webHost.Initialize(options, routingHandler);
    }

    /// <summary>
    /// Adds a global instruction that applies to all API requests.
    /// </summary>
    /// <param name="instruction">The instruction to guide AI response generation (e.g., "Return error responses for invalid card numbers").</param>
    /// <remarks>
    /// Global instructions are applied to all requests regardless of the route or HTTP method.
    /// Multiple instructions can be added and they will all be considered when generating responses.
    /// </remarks>
    public void AddInstruction(string instruction)
    {
        instructionsContainer.AddInstruction(instruction);
    }

    /// <summary>
    /// Clears all global instructions that were previously added.
    /// </summary>
    /// <remarks>
    /// This does not clear route-specific instructions configured via <see cref="ConfigureRoutes"/>.
    /// </remarks>
    public void ClearInstructions()
    {
        instructionsContainer.ClearInstructions();
    }

    /// <summary>
    /// Gets the name of the API being simulated.
    /// </summary>
    public string ApiName => apiName;

    /// <summary>
    /// Changes the base cache folder path for this simulator.
    /// </summary>
    /// <param name="path">The new base path for cache storage.</param>
    public void SetCacheBasePath(string path)
    {
        fileManager.SetCacheBasePath(path);
    }

    /// <summary>
    /// Gets the instructions container for this simulator instance.
    /// </summary>
    internal InstructionsContainer InstructionsContainer => instructionsContainer;

    /// <summary>
    /// Gets the routing request handler for this simulator instance.
    /// Used by Augustus.AI to install default handlers.
    /// </summary>
    internal RoutingRequestHandler RoutingHandler => routingHandler;

    /// <summary>
    /// Gets the file manager for cache operations.
    /// Used by Augustus.AI default handlers for cache read/write.
    /// </summary>
    internal FileManager CacheFileManager => fileManager;

    /// <summary>
    /// Gets the configuration options for this simulator instance.
    /// </summary>
    internal APISimulatorOptions Options => options;

    /// <summary>
    /// Starts the API simulator web server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <remarks>
    /// After calling this method, the simulator will be listening for HTTP requests on the configured port.
    /// Use <see cref="CreateClient"/> to get an HttpClient configured to communicate with the simulator.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the server is already started or if the port is already in use.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await webHost.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the API simulator web server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    /// <remarks>
    /// After calling this method, the simulator will no longer accept HTTP requests.
    /// Any active connections will be closed gracefully.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await webHost.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an HttpClient configured to communicate with the simulator.
    /// </summary>
    /// <returns>A new <see cref="HttpClient"/> instance pointed at the simulator's endpoint.</returns>
    /// <remarks>
    /// Each call creates a new HttpClient instance. The client's BaseAddress is set to the simulator's URL.
    /// The caller is responsible for disposing of the returned HttpClient.
    /// </remarks>
    public HttpClient CreateClient()
    {
        return webHost.CreateClient();
    }

    /// <summary>
    /// Sets the current test context, routing cache operations to a per-test subdirectory.
    /// </summary>
    /// <remarks>
    /// In BDD frameworks like SpecFlow/Reqnroll, call this in [BeforeScenario] to organize
    /// cache files by scenario name. Each scenario gets its own subdirectory under the cache folder.
    /// </remarks>
    /// <param name="testName">The test or scenario name (e.g., ScenarioContext.ScenarioInfo.Title).</param>
    public void SetTestContext(string testName)
    {
        fileManager.SetTestContext(testName);
    }

    /// <summary>
    /// Clears the current test context and runs scoped stale cache removal for that context's subdirectory.
    /// </summary>
    /// <remarks>
    /// In BDD frameworks like SpecFlow/Reqnroll, call this in [AfterScenario] to clean up
    /// stale cache entries for the completed scenario.
    /// </remarks>
    public void ClearTestContext()
    {
        fileManager.ClearTestContext();
    }

    /// <summary>
    /// Clears all cached API responses from disk.
    /// </summary>
    /// <remarks>
    /// Cached responses are stored as JSON files in the configured cache folder.
    /// After clearing the cache, subsequent requests will generate fresh responses from OpenAI.
    /// This is useful during test development when you want to regenerate responses with new instructions.
    /// </remarks>
    public void ClearCache()
    {
        fileManager.ClearCache();
    }

    /// <summary>
    /// Creates a builder for configuring route-specific instructions.
    /// </summary>
    /// <returns>An <see cref="InstructionBuilder"/> instance for fluent configuration of route patterns and instructions.</returns>
    /// <remarks>
    /// Route-specific instructions allow you to provide different guidance for different API endpoints.
    /// Use the builder's methods like <c>ForRoute</c>, <c>ForGet</c>, <c>ForPost</c> to specify patterns.
    /// </remarks>
    /// <example>
    /// <code>
    /// simulator.ConfigureRoutes()
    ///     .ForGet("/api/customers/{id}")
    ///     .WithInstruction("Return a customer object with the specified ID")
    ///     .Build();
    /// </code>
    /// </example>
    public InstructionBuilder ConfigureRoutes()
    {
        return new InstructionBuilder(this);
    }

    /// <summary>
    /// Asynchronously disposes of the simulator and releases all resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    /// <remarks>
    /// This method stops the web server if it's running and releases all managed resources.
    /// It's safe to call this method multiple times; subsequent calls will have no effect.
    /// Consider using the 'await using' pattern for automatic disposal.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        try
        {
            await webHost.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log but don't throw during disposal
            System.Diagnostics.Debug.WriteLine($"Error during APISimulator disposal: {ex}");
        }

        if (options.AutoRemoveStaleCache && options.EnableCaching)
        {
            try
            {
                fileManager.RemoveStaleEntries();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during stale cache removal: {ex}");
            }
        }

        disposed = true;
        GC.SuppressFinalize(this);
    }
}
