using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace SimpleR.Internal;

internal partial class WebSocketConnectionHandler<THandler, TMessage>
    where THandler : IWebSocketMessageDispatcher<TMessage>
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "Unable to remove the connection handler({HandlerId}) from the Server Context.", EventName = "UnableToRemoveConnection")]
        public static partial void UnableToRemoveConnection(ILogger logger, string handlerId);

        [LoggerMessage(2, LogLevel.Debug, "Unable to process the connection.", EventName = "UnableToProcessConnection")]
        public static partial void UnableToProcessConnection(ILogger logger, Exception exception);

        [LoggerMessage(3, LogLevel.Debug, "Unable to process the message.", EventName = "UnableToProcessMessage")]
        public static partial void UnableToProcessMessage(ILogger logger, Exception exception);
    }
}
