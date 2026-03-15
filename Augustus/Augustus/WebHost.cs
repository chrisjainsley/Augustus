namespace Augustus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class WebHost : IAsyncDisposable
{
    private string bindUrl = "http://localhost:9001";
    private string? resolvedUrl;
    private IHost? webHost;
    private IRequestHandler? requestHandler;
    private APISimulatorOptions? options;
    private readonly SemaphoreSlim startStopLock = new SemaphoreSlim(1, 1);
    private bool disposed;

    public void Initialize(APISimulatorOptions options, IRequestHandler requestHandler)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        // Use 127.0.0.1 instead of localhost when port is 0 (Kestrel requires explicit IP for dynamic port binding)
        var host = options.Port == 0 ? "127.0.0.1" : "localhost";
        this.bindUrl = $"http://{host}:{options.Port}";
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await startStopLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (webHost != null)
                throw new InvalidOperationException("WebHost is already started. Call StopAsync() before starting again.");

            if (requestHandler == null)
                throw new InvalidOperationException("WebHost must be initialized before starting");

            webHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(bindUrl);
                    webBuilder.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await requestHandler.HandleAsync(context, context.RequestAborted);
                        });
                    });
                })
                .Build();

            await webHost.StartAsync(cancellationToken).ConfigureAwait(false);

            // Resolve the actual listening URL (important when port 0 is used for auto-assignment)
            var server = webHost.Services.GetRequiredService<IServer>();
            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            resolvedUrl = addressFeature?.Addresses.FirstOrDefault() ?? bindUrl;
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
            if (requestHandler != null)
            {
                await requestHandler.DrainPendingCacheWritesAsync(cancellationToken).ConfigureAwait(false);
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

        return new HttpClient() { BaseAddress = new Uri(resolvedUrl ?? bindUrl) };
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

        (requestHandler as IDisposable)?.Dispose();
        startStopLock.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }
}
