using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace SimpleR;

internal partial class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocketConnectionContext> _connections = new();
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly ILogger<WebSocketConnectionContext> _connectionLogger;

    public WebSocketConnectionManager(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<WebSocketConnectionManager>();
        _connectionLogger = loggerFactory.CreateLogger<WebSocketConnectionContext>();
    }

    internal bool TryGetConnection(string id, out WebSocketConnectionContext? connection) => _connections.TryGetValue(id, out connection);

    internal WebSocketConnectionContext CreateConnection() => CreateConnection(new());

    internal WebSocketConnectionContext CreateConnection(HttpConnectionDispatcherOptions options)
    {
        var id = MakeNewConnectionId();

        Log.CreatedNewConnection(_logger, id);
        var transportPipeOptions = new PipeOptions(pauseWriterThreshold: options.TransportMaxBufferSize, resumeWriterThreshold: options.TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
        var appPipeOptions = new PipeOptions(pauseWriterThreshold: options.ApplicationMaxBufferSize, resumeWriterThreshold: options.ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(transportPipeOptions, appPipeOptions);
        var connection = new WebSocketConnectionContext(id, _connectionLogger, pair.Application, pair.Transport, options);

        _connections.TryAdd(id, connection);

        return connection;
    }

    internal async Task DisposeAndRemoveAsync(WebSocketConnectionContext connection, bool closeGracefully)
    {
        try
        {
            await connection.DisposeAsync(closeGracefully);
        }
        catch (IOException ex)
        {
            Log.ConnectionReset(_logger, connection.ConnectionId, ex);
        }
        catch (WebSocketException ex) when (ex.InnerException is IOException)
        {
            Log.ConnectionReset(_logger, connection.ConnectionId, ex);
        }
        catch (Exception ex)
        {
            Log.FailedDispose(_logger, connection.ConnectionId, ex);
        }
        finally
        {
            // Remove it from the list after disposal so that's it's easy to see
            // connections that might be in a hung state via the connections list
            RemoveConnection(connection.ConnectionId);
        }
    }

    public void RemoveConnection(string id)
    {
        if (_connections.TryRemove(id, out var _))
        {
            Log.RemovedConnection(_logger, id);
        }
    }

    private static string MakeNewConnectionId()
    {
        // 128 bit buffer / 8 bits per byte = 16 bytes
        Span<byte> buffer = stackalloc byte[16];
        // Generate the id with RNGCrypto because we want a cryptographically random id, which GUID is not
        RandomNumberGenerator.Fill(buffer);
        return WebEncoders.Base64UrlEncode(buffer);
    }
}
