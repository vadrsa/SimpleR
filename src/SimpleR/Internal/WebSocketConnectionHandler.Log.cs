using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace SimpleR.Internal;

internal partial class WebSocketConnectionHandler<TMessage>
{
    private static partial class Log
    {
        
        [LoggerMessage(1, LogLevel.Error, "Error when dispatching '{HubMethod}' on hub.", EventName = "ErrorDispatchingEvent")]
        public static partial void ErrorDispatchingEvent(ILogger logger, string hubMethod, Exception exception);

        [LoggerMessage(2, LogLevel.Debug, "Error when processing requests.", EventName = "ErrorProcessingRequest")]
        public static partial void ErrorProcessingRequest(ILogger logger, Exception exception);

        [LoggerMessage(3, LogLevel.Debug, "OnConnectedAsync started.", EventName = "ConnectedStarting")]
        public static partial void ConnectedStarting(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "OnConnectedAsync ending.", EventName = "ConnectedEnding")]
        public static partial void ConnectedEnding(ILogger logger);
    }
}