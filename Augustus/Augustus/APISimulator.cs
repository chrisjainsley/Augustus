namespace Augustus;

using System;
using System.Threading.Tasks;

/// <summary>
/// An AI-powered HTTP API simulator that generates realistic responses using OpenAI.
/// </summary>
/// <remarks>
/// The API simulator creates a local web server that intercepts HTTP requests and generates
/// appropriate responses based on instructions provided. It uses OpenAI's language models to
/// create realistic API responses and caches them for performance.
/// Implements <see cref="IAsyncDisposable"/> for proper resource cleanup.
/// </remarks>
/// <example>
/// <code>
/// var options = new APISimulatorOptions
/// {
///     OpenAIApiKey = "your-key",
///     Port = 9001
/// };
/// await using var simulator = new APISimulator("Stripe", options);
/// simulator.AddInstruction("Return realistic Stripe API responses");
/// await simulator.StartAsync();
/// var client = simulator.CreateClient();
/// // Make requests to the client...
/// // DisposeAsync will be called automatically
/// </code>
/// </example>
public partial class APISimulator : IAsyncDisposable
{
    private readonly string apiName;
    private readonly APISimulatorOptions options;
    private readonly WebHost webHost = new();
    private readonly InstructionsContainer instructionsContainer;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="APISimulator"/> class.
    /// </summary>
    /// <param name="apiName">The name of the API being simulated (e.g., "Stripe", "PayPal"). Used for context in AI responses.</param>
    /// <param name="options">Configuration options for the simulator, including OpenAI API key and port settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiName"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">Thrown when options validation fails (e.g., missing API key or invalid endpoint).</exception>
    public APISimulator(string apiName, APISimulatorOptions options)
    {
        this.apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        // Validate options early (fail-fast) instead of waiting until first request
        options.Validate();

        instructionsContainer = new InstructionsContainer(apiName);
        webHost.Initialize(options, instructionsContainer);
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
    /// Gets the instructions container for this simulator instance.
    /// </summary>
    internal InstructionsContainer InstructionsContainer => instructionsContainer;

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
        await webHost.StartAsync(cancellationToken);
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
        await webHost.StopAsync(cancellationToken);
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
    /// Clears all cached API responses from disk.
    /// </summary>
    /// <remarks>
    /// Cached responses are stored as JSON files in the configured cache folder.
    /// After clearing the cache, subsequent requests will generate fresh responses from OpenAI.
    /// This is useful during test development when you want to regenerate responses with new instructions.
    /// </remarks>
    public void ClearCache()
    {
        var fileManager = new FileManager(options.CacheFolderPath);
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
            await webHost.DisposeAsync();
        }
        catch (Exception ex)
        {
            // Log but don't throw during disposal
            System.Diagnostics.Debug.WriteLine($"Error during APISimulator disposal: {ex}");
        }

        disposed = true;
        GC.SuppressFinalize(this);
    }
}
