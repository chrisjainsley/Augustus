namespace Augustus;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Response strategy that loads JSON responses from a file.
/// </summary>
internal class FileResponseStrategy : IResponseStrategy
{
    private readonly string filePath;
    private readonly int statusCode;
    private readonly Lazy<string> cachedContent;

    public FileResponseStrategy(string filePath, int statusCode = 200)
    {
        this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        this.statusCode = statusCode;
        cachedContent = new Lazy<string>(() => File.ReadAllText(filePath));
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(cachedContent.Value, cancellationToken);
    }
}
