using Microsoft.AspNetCore.Http;

namespace SimpleR.Internal;

internal interface IHttpTransport
{
    /// <summary>
    /// Executes the transport
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    /// <returns>A <see cref="Task"/> that completes when the transport has finished processing</returns>
    Task<bool> ProcessRequestAsync(HttpContext context, CancellationToken token);
}