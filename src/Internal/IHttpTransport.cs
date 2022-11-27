using Microsoft.AspNetCore.Http;

namespace SimpleR.Internal;

internal interface IHttpTransport
{
    Task ProcessRequestAsync(HttpContext context, CancellationToken token);
}
