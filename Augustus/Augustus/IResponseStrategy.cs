namespace Augustus;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a strategy for generating HTTP responses for mock server routes.
/// </summary>
public interface IResponseStrategy
{
    /// <summary>
    /// Generates a response for the given HTTP request.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing the request and response.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async response generation operation.</returns>
    Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
