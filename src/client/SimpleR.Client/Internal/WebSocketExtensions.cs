using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static class WebSocketExtensions
    {
        public static ValueTask SendAsync(this WebSocket webSocket, ReadOnlySequence<byte> buffer, WebSocketMessageType webSocketMessageType, bool isEndOfMessage, CancellationToken cancellationToken = default)
        {
            if (buffer.IsSingleSegment)
            {
                return webSocket.SendAsync(buffer.First, webSocketMessageType, endOfMessage: isEndOfMessage, cancellationToken);
            }
            else
            {
                return SendMultiSegmentAsync(webSocket, buffer, webSocketMessageType, isEndOfMessage, cancellationToken);
            }
        }

        private static async ValueTask SendMultiSegmentAsync(WebSocket webSocket, ReadOnlySequence<byte> buffer, WebSocketMessageType webSocketMessageType, bool isEndOfMessage,  CancellationToken cancellationToken = default)
        {
            var position = buffer.Start;
            // Get a segment before the loop so we can be one segment behind while writing
            // This allows us to do a non-zero byte write for the endOfMessage = true send
            buffer.TryGet(ref position, out var prevSegment);
            while (buffer.TryGet(ref position, out var segment))
            {
                await webSocket.SendAsync(prevSegment, webSocketMessageType, endOfMessage: false, cancellationToken).ConfigureAwait(false);

                prevSegment = segment;
            }

            // End of message frame
            await webSocket.SendAsync(prevSegment, webSocketMessageType, endOfMessage: isEndOfMessage, cancellationToken).ConfigureAwait(false);
        }
    }

}
