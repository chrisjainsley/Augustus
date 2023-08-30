namespace Augustus;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

internal class WebHost
{
    private const string Url = "http://localhost:9001";
    private IHost? webHost;
    private readonly ResponseGenerator responseGenerator = new();

    public async Task StartAsync()
    {
        webHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(Url);
                webBuilder.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        await responseGenerator.GenerateResponse(context);
                    });
                });
            })
            .Build();
             
        await webHost.StartAsync();
    }

    public async Task StopAsync()
    {
        if (webHost == null) return;

        await webHost.StopAsync();
    }

    public HttpClient CreateClient()
    {
        return new HttpClient() { BaseAddress = new Uri(Url) };
    }

}
