namespace Augustus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

/// <summary>
/// Internal web host for the mock server.
/// </summary>
internal class MockWebHost : IAsyncDisposable
{
    private readonly string url;
    private readonly MockServer mockServer;
    private readonly SemaphoreSlim startStopLock = new(1, 1);
    private IHost? webHost;
    private bool disposed;

    public MockWebHost(MockServerOptions options, MockServer mockServer)
    {
        this.url = $"http://localhost:{options.Port}";
        this.mockServer = mockServer ?? throw new ArgumentNullException(nameof(mockServer));
    }

    public bool IsRunning => webHost != null;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await startStopLock.WaitAsync(cancellationToken);
        try
        {
            if (webHost != null)
                throw new InvalidOperationException("WebHost is already started. Call StopAsync() before starting again.");

            webHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(url);
                    webBuilder.Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await HandleRequestAsync(context, context.RequestAborted);
                        });
                    });
                })
                .Build();

            await webHost.StartAsync(cancellationToken);
        }
        finally
        {
            startStopLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await startStopLock.WaitAsync(cancellationToken);
        try
        {
            if (webHost == null)
                return;

            await webHost.StopAsync(cancellationToken);

            if (webHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
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

    private async Task HandleRequestAsync(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            var path = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;

            var route = mockServer.GetRouteForRequest(path, method);

            if (route?.ResponseStrategy == null)
            {
                await WriteNotFoundResponse(context, path, method, cancellationToken);
                return;
            }

            await route.ResponseStrategy.GenerateResponseAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteErrorResponse(context, $"Internal error: {ex.Message}", 500, cancellationToken);
        }
    }

    private async Task WriteNotFoundResponse(HttpContext context, string path, string method, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new
        {
            error = $"No route configured for {method} {path}",
            status = 404
        });
        await context.Response.WriteAsync(errorResponse, cancellationToken);
    }

    private async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new { error = message, status = statusCode });
        await context.Response.WriteAsync(errorResponse, cancellationToken);
    }

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
            System.Diagnostics.Debug.WriteLine($"Error during MockWebHost disposal: {ex}");
        }

        disposed = true;
        startStopLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
