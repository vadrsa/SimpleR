using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SimpleR.Protocol;

namespace SimpleR.Client.Internal
{
    public class WebSocketClientTransport
    {
        private readonly WebsocketClientOptions _options;
        private readonly IDuplexPipe _application;
        private volatile bool _aborted;

        public WebSocketClientTransport(WebsocketClientOptions options, IDuplexPipe application)
        {
            _options = options;
            _application = application;
        }

        public async Task ProcessSocketAsync(WebSocket socket, CancellationToken cancellationToken = default)
        {
            // Begin sending and receiving. Receiving must be started first because ExecuteAsync enables SendAsync.
            var receiving = StartReceiving(socket,cancellationToken);
            var sending = StartSending(socket);

            // Wait for send or receive to complete
            var trigger = await Task.WhenAny(receiving, sending);

            if (trigger == receiving)
            {
                // Log.WaitingForSend(_logger);

                // We're waiting for the application to finish and there are 2 things it could be doing
                // 1. Waiting for application data
                // 2. Waiting for a websocket send to complete

                // Cancel the application so that ReadAsync yields
                _application.Input.CancelPendingRead();

                using (var delayCts = new CancellationTokenSource())
                {
                    var resultTask = await Task.WhenAny(sending, Task.Delay(_options.CloseTimeout, delayCts.Token));

                    if (resultTask != sending)
                    {
                        // We timed out so now we're in ungraceful shutdown mode
                        // Log.CloseTimedOut(_logger);

                        // Abort the websocket if we're stuck in a pending send to the client
                        _aborted = true;

                        socket.Abort();
                    }
                    else
                    {
                        delayCts.Cancel();
                    }
                }
            }
            else
            {
                // Log.WaitingForClose(_logger);

                // We're waiting on the websocket to close and there are 2 things it could be doing
                // 1. Waiting for websocket data
                // 2. Waiting on a flush to complete (backpressure being applied)

                using (var delayCts = new CancellationTokenSource())
                {
                    var resultTask = await Task.WhenAny(receiving, Task.Delay(_options.CloseTimeout, delayCts.Token));

                    if (resultTask != receiving)
                    {
                        // Abort the websocket if we're stuck in a pending receive from the client
                        _aborted = true;

                        socket.Abort();

                        // Cancel any pending flush so that we can quit
                        _application.Output.CancelPendingFlush();
                    }
                    else
                    {
                        delayCts.Cancel();
                    }
                }
            }
        }
        
        private async Task StartReceiving(WebSocket socket, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Do a 0 byte read so that idle connections don't allocate a buffer when waiting for a read
                    var result = await socket.ReceiveAsync(Memory<byte>.Empty, token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    var memory = _application.Output.GetMemory();

                    var receiveResult = await socket.ReceiveAsync(memory, token);

                    // Need to check again for netcoreapp3.0 and later because a close can happen between a 0-byte read and the actual read
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    // Log.MessageReceived(_logger, receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);
                    _options.EndOfMessageBytes.CopyTo(memory.Slice(receiveResult.Count));
                    _application.Output.Advance(receiveResult.Count + _options.EndOfMessageBytes.Length);

                    var flushResult = await _application.Output.FlushAsync();

                    // We canceled in the middle of applying back pressure
                    // or if the consumer is done
                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                // Client has closed the WebSocket connection without completing the close handshake
                // Log.ClosedPrematurely(_logger, ex);
            }
            catch (OperationCanceledException)
            {
                // Ignore aborts, don't treat them like transport errors
            }
            catch (Exception ex)
            {
                if (!_aborted && !token.IsCancellationRequested)
                {
                    _application.Output.Complete(ex);
                }
            }
            finally
            {
                // We're done writing
                _application.Output.Complete();
            }
        }

        private async Task StartSending(WebSocket socket)
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    var result = await _application.Input.ReadAsync();
                    var buffer = result.Buffer;
                    long advanceBytes = 0;
                    
                    // Get a frame from the application
                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            try
                            {

                                var webSocketMessageType = (_options.TransferFormat == TransferFormat.Binary
                                    ? WebSocketMessageType.Binary
                                    : WebSocketMessageType.Text);

                                if (WebSocketCanSend(socket))
                                {
                                    advanceBytes = ParseEndOfMessage(ref buffer, out var sendBuffer, out var isEndOfMessage);
                                    // Log.SendPayload(_logger, sendBuffer.Length);
                                    await socket.SendAsync(sendBuffer, webSocketMessageType, isEndOfMessage);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!_aborted)
                                {
                                    // Log.ErrorWritingFrame(_logger, ex);
                                }
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _application.Input.AdvanceTo(buffer.GetPosition(advanceBytes), buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                // Send the close frame before calling into user code
                if (WebSocketCanSend(socket))
                {
                    try
                    {
                        // We're done sending, send the close frame to the client if the websocket is still open
                        await socket.CloseOutputAsync(error != null ? WebSocketCloseStatus.InternalServerError : WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        // Log.ClosingWebSocketFailed(_logger, ex);
                    }
                }

                _application.Input.Complete();
            }
        }

        private long ParseEndOfMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> sendBuffer, out bool isEndOfMessage)
        {
            if (_options.EndOfMessageBytes.Length == 0)
            {
                isEndOfMessage = false;
                sendBuffer = buffer;
                return buffer.Length;
            }

            var reader = new SequenceReader<byte>(buffer);
            if (reader.TryReadTo(out ReadOnlySequence<byte> newBuffer, new ReadOnlySpan<byte>(_options.EndOfMessageBytes)))
            {
                var size = reader.Consumed;
                sendBuffer = newBuffer;
                isEndOfMessage = true;
                return size;
            }
            
            isEndOfMessage = false;
            sendBuffer = ReadOnlySequence<byte>.Empty;
            return 0;
        }

        private static bool WebSocketCanSend(WebSocket ws)
        {
            return !(ws.State == WebSocketState.Aborted ||
                   ws.State == WebSocketState.Closed ||
                   ws.State == WebSocketState.CloseSent);
        }
    }
}
