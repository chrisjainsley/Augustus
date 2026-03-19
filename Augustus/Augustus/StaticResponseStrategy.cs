namespace Augustus;

using Microsoft.AspNetCore.Http;
using System.Text.Json;

/// <summary>
/// Response strategy that returns a static JSON response.
/// </summary>
internal class StaticResponseStrategy : IResponseStrategy
{
    private readonly string jsonResponse;
    private readonly int statusCode;

    public StaticResponseStrategy(string jsonResponse, int statusCode = 200)
    {
        this.jsonResponse = jsonResponse ?? throw new ArgumentNullException(nameof(jsonResponse));
        this.statusCode = statusCode;
    }

    public StaticResponseStrategy(object responseObject, int statusCode = 200)
    {
        if (responseObject == null)
            throw new ArgumentNullException(nameof(responseObject));

        this.jsonResponse = JsonSerializer.Serialize(responseObject);
        this.statusCode = statusCode;
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(jsonResponse, cancellationToken);
    }
}
