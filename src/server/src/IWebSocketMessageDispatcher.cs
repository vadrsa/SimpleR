namespace SimpleR;

/// <summary>
/// Message dispatcher for handling websocket connections.
/// </summary>
/// <typeparam name="TMessage">Message type</typeparam>
public interface IWebSocketMessageDispatcher<TMessage>
{
    /// <summary>
    /// Called when a connection is established.
    /// </summary>
    /// <param name="connection">The connection.</param>
    Task OnConnectedAsync(IWebsocketConnectionContext<TMessage> connection);

    /// <summary>
    /// Called when a connection is disconnected.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    Task OnDisconnectedAsync(IWebsocketConnectionContext<TMessage> connection, Exception? exception);

    /// <summary>
    /// Dispatches a message to the application.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="message">The message to dispatch.</param>
    Task DispatchMessageAsync(IWebsocketConnectionContext<TMessage> connection, TMessage message);
}