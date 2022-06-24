using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.Logging;
using System.IO.Pipelines;
using System.Net.WebSockets;

namespace SimpleR;

internal partial class WebSocketsServerTransport : IHttpTransport
{
    private readonly WebSocketOptions _options;
    private readonly IDuplexPipe _application;
    private readonly ILogger _logger;
    private readonly WebSocketConnectionContext _connection;
    private volatile bool _aborted;

    public WebSocketsServerTransport(WebSocketOptions options,
        IDuplexPipe application,
        WebSocketConnectionContext connection,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _application = application;
        _connection = connection;

        _logger = loggerFactory.CreateLogger<WebSocketsServerTransport>();
    }

    public async Task ProcessRequestAsync(HttpContext context, CancellationToken token)
    {
        var subProtocol = _options.SubProtocolSelector?.Invoke(context.WebSockets.WebSocketRequestedProtocols);

        using var ws = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
        Log.SocketOpened(_logger, subProtocol);

        try
        {
            await ProcessSocketAsync(ws);
        }
        finally
        {
            Log.SocketClosed(_logger);
        }
    }

    public async Task ProcessSocketAsync(WebSocket socket)
    {
        var receiving = StartReceivingAsync(socket);
        var sending = StartSendingAsync(socket);

        // Wait for send or receive to complete
        var trigger = await Task.WhenAny(receiving, sending);

        if (trigger == receiving)
        {
            // We're waiting for the application to finish and there are 2 things it could be doing
            // 1. Waiting for application data
            // 2. Waiting for a websocket send to complete

            // Cancel the application so that ReadAsync yields
            _application.Input.CancelPendingRead();

            using var delayCts = new CancellationTokenSource();
            var resultTask = await Task.WhenAny(sending, Task.Delay(5000, delayCts.Token));

            if (resultTask != sending)
            {
                // We timed out so now we're in ungraceful shutdown mode

                // Abort the websocket if we're stuck in a pending send to the client
                _aborted = true;

                socket.Abort();
            }
            else
            {
                delayCts.Cancel();
            }
        }
        else
        {
            // We're waiting on the websocket to close and there are 2 things it could be doing
            // 1. Waiting for websocket data
            // 2. Waiting on a flush to complete (backpressure being applied)

            using var delayCts = new CancellationTokenSource();
            var resultTask = await Task.WhenAny(receiving, Task.Delay(5000, delayCts.Token));

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

    private async Task StartReceivingAsync(WebSocket socket)
    {
        var token = _connection.Cancellation?.Token ?? default;

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

                Log.MessageReceived(_logger, receiveResult.MessageType, receiveResult.Count, receiveResult.EndOfMessage);

                _application.Output.Advance(receiveResult.Count);

                _connection.IsEndOfMessage = receiveResult.EndOfMessage;
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
            Log.ClosedPrematurely(_logger, ex);
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

    private async Task StartSendingAsync(WebSocket socket)
    {
        Exception? error = null;

        try
        {
            while (true)
            {
                var result = await _application.Input.ReadAsync();
                var buffer = result.Buffer;

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
                            Log.SendPayload(_logger, buffer.Length);

                            var webSocketMessageType = _connection.ActiveFormat == TransferFormat.Binary
                                ? WebSocketMessageType.Binary
                                : WebSocketMessageType.Text;

                            if (WebSocketCanSend(socket))
                            {
                                await socket.SendAsync(buffer, webSocketMessageType /* TODO : Add cancel*/);
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
                                Log.ErrorWritingFrame(_logger, ex);
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
                    _application.Input.AdvanceTo(buffer.End);
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
                    Log.ClosingWebSocketFailed(_logger, ex);
                }
            }

            _application.Input.Complete();
        }
    }

    private static bool WebSocketCanSend(WebSocket ws)
    {
        return ws.State is not (WebSocketState.Aborted or
                WebSocketState.Closed or
                WebSocketState.CloseSent);
    }

}
