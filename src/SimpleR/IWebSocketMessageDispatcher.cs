namespace SimpleR;

/// <summary>
/// Message dispatcher for handling websocket connections.
/// </summary>
/// <typeparam name="TMessageIn">Input Message type</typeparam>
/// <typeparam name="TMessageOut">Output Message type</typeparam>
public interface IWebSocketMessageDispatcher<in TMessageIn, out TMessageOut>
{
    /// <summary>
    /// Called when a connection is established.
    /// </summary>
    /// <param name="connection">The connection.</param>
    Task OnConnectedAsync(IWebsocketConnectionContext<TMessageOut> connection);

    /// <summary>
    /// Called when a connection is disconnected.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    Task OnDisconnectedAsync(IWebsocketConnectionContext<TMessageOut> connection, Exception? exception);

    /// <summary>
    /// Dispatches a message to the application.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="message">The message to dispatch.</param>
    Task DispatchMessageAsync(IWebsocketConnectionContext<TMessageOut> connection, TMessageIn message);


    /// <summary>
    /// Handle errors that happened while parsing messages
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="exception">The exception that occurred.</param>
    Task OnParsingIssueAsync(IWebsocketConnectionContext<TMessageOut> connection, Exception exception) =>
        Task.CompletedTask;


}

/// <summary>
/// Message dispatcher for handling websocket connections.
/// </summary>
/// <typeparam name="TMessage">Message type</typeparam>
public interface IWebSocketMessageDispatcher<TMessage> : IWebSocketMessageDispatcher<TMessage, TMessage>;