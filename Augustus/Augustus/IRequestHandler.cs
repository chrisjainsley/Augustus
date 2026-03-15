using Microsoft.AspNetCore.Http;

namespace Augustus;

internal interface IRequestHandler
{
    Task HandleAsync(HttpContext context, CancellationToken cancellationToken);
    Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken);
}
