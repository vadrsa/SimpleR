using System;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace SimpleR.Internal;

internal sealed partial class WebSocketsServerTransport
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Socket opened using Sub-Protocol: '{SubProtocol}'.", EventName = "SocketOpened")]
        public static partial void SocketOpened(ILogger logger, string? subProtocol);

        [LoggerMessage(2, LogLevel.Debug, "Socket closed.", EventName = "SocketClosed")]
        public static partial void SocketClosed(ILogger logger);

        [LoggerMessage(4, LogLevel.Debug, "Waiting for the application to finish sending data.", EventName = "WaitingForSend")]
        public static partial void WaitingForSend(ILogger logger);

        [LoggerMessage(7, LogLevel.Debug, "Waiting for the client to close the socket.", EventName = "WaitingForClose")]
        public static partial void WaitingForClose(ILogger logger);

        [LoggerMessage(8, LogLevel.Debug, "Timed out waiting for client to send the close frame, aborting the connection.", EventName = "CloseTimedOut")]
        public static partial void CloseTimedOut(ILogger logger);

        [LoggerMessage(9, LogLevel.Trace, "Message received. Type: {MessageType}, size: {Size}, EndOfMessage: {EndOfMessage}.", EventName = "MessageReceived")]
        public static partial void MessageReceived(ILogger logger, WebSocketMessageType messageType, int size, bool endOfMessage);

        [LoggerMessage(11, LogLevel.Trace, "Sending payload: {Size} bytes.", EventName = "SendPayload")]
        public static partial void SendPayload(ILogger logger, long size);

        [LoggerMessage(12, LogLevel.Debug, "Error writing frame.", EventName = "ErrorWritingFrame")]
        public static partial void ErrorWritingFrame(ILogger logger, Exception ex);

        [LoggerMessage(14, LogLevel.Debug, "Socket connection closed prematurely.", EventName = "ClosedPrematurely")]
        public static partial void ClosedPrematurely(ILogger logger, Exception ex);

        [LoggerMessage(15, LogLevel.Debug, "Closing webSocket failed.", EventName = "ClosingWebSocketFailed")]
        public static partial void ClosingWebSocketFailed(ILogger logger, Exception ex);

        [LoggerMessage(16, LogLevel.Debug, "Send loop errored.", EventName = "SendErrored")]
        public static partial void SendErrored(ILogger logger, Exception exception);
    }
}