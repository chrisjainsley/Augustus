using Microsoft.AspNetCore.Http;

namespace Augustus;

internal interface IResponseGenerator
{
    Task GenerateResponse(HttpContext httpContext, CancellationToken cancellationToken = default);
    Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken = default);
}
