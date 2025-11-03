namespace Augustus;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Response strategy that loads JSON responses from a file.
/// </summary>
internal class FileResponseStrategy : IResponseStrategy
{
    private readonly string filePath;
    private readonly int statusCode;
    private string? cachedContent;

    public FileResponseStrategy(string filePath, int statusCode = 200)
    {
        this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        this.statusCode = statusCode;
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        // Load and cache the file content on first use
        if (cachedContent == null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Response file not found: {filePath}");
            }

            cachedContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(cachedContent, cancellationToken);
    }
}
