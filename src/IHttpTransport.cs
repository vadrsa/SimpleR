using Microsoft.AspNetCore.Http;

namespace SimpleR;

internal interface IHttpTransport
{
    Task ProcessRequestAsync(HttpContext context, CancellationToken token);
}
