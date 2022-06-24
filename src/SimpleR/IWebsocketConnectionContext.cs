using System.Security.Claims;

namespace SimpleR;
/// <summary>
/// Encapsulates a websocket connection to a client.
/// </summary>
public interface IWebsocketConnectionContext
{
    /// <summary>
    /// Gets the unique identifier for the connection.
    /// </summary>
    string ConnectionId { get; }

    /// <summary>
    /// Gets the user associated with the connection.
    /// </summary>
    ClaimsPrincipal User { get; }

    /// <summary>
    /// Aborts the connection.
    /// </summary>
    void Abort();
}

/// <summary>
/// Encapsulates a websocket connection to a client.
/// </summary>
public interface IWebsocketConnectionContext<in TMessageOut> : IWebsocketConnectionContext
{
    /// <summary>
    /// Writes a message to the connection.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    ValueTask WriteAsync(TMessageOut message, CancellationToken cancellationToken = default);
}