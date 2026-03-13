namespace Augustus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

internal class WebHost : IAsyncDisposable
{
    private string url = "http://localhost:9001";
    private IHost? webHost;
    private ResponseGenerator? responseGenerator;
    private APISimulatorOptions? options;
    private readonly SemaphoreSlim startStopLock = new SemaphoreSlim(1, 1);
    private bool disposed;

    public void Initialize(APISimulatorOptions options, InstructionsContainer instructionsContainer, APISimulator.FileManager fileManager)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.url = $"http://localhost:{options.Port}";
        this.responseGenerator = new ResponseGenerator(options, instructionsContainer, fileManager);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await startStopLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (webHost != null)
                throw new InvalidOperationException("WebHost is already started. Call StopAsync() before starting again.");

            if (responseGenerator == null)
                throw new InvalidOperationException("WebHost must be initialized before starting");

            webHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(url);
                    webBuilder.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await responseGenerator.GenerateResponse(context, context.RequestAborted);
                        });
                    });
                })
                .Build();

            await webHost.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            startStopLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await startStopLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (webHost == null)
                return;

            // First stop the web host so it no longer accepts or processes requests
            await webHost.StopAsync(cancellationToken).ConfigureAwait(false);

            // Then drain any in-flight background cache writes that were queued before shutdown
            if (responseGenerator != null)
            {
                await responseGenerator.DrainPendingCacheWritesAsync(cancellationToken).ConfigureAwait(false);
            }

            // Dispose of the host after stopping
            if (webHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else if (webHost is IDisposable disposable)
            {
                disposable.Dispose();
            }

            webHost = null;
        }
        finally
        {
            startStopLock.Release();
        }
    }

    public HttpClient CreateClient()
    {
        if (webHost == null)
            throw new InvalidOperationException("WebHost must be started before creating clients. Call StartAsync() first.");

        return new HttpClient() { BaseAddress = new Uri(url) };
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log but don't throw during disposal
            System.Diagnostics.Debug.WriteLine($"Error during WebHost disposal: {ex}");
        }

        startStopLock.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }
}
