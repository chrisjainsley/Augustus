namespace Augustus;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Response strategy that loads JSON responses from a file.
/// </summary>
internal class FileResponseStrategy : IResponseStrategy
{
    private readonly string cachedContent;
    private readonly int statusCode;

    public FileResponseStrategy(string filePath, int statusCode = 200)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));
        this.statusCode = statusCode;
        cachedContent = File.ReadAllText(filePath);
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(cachedContent, cancellationToken);
    }
}
