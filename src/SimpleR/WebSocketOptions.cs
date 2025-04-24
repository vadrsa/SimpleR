using SimpleR.Protocol;

namespace SimpleR;

public class WebSocketOptions
{
    /// <summary>
    /// Gets or sets the amount of time the WebSocket transport will wait for a graceful close before starting an ungraceful close.
    /// </summary>
    /// <value>Defaults to 5 seconds</value>
    public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets a delegate that will be called when a new WebSocket is established to select the value
    /// for the 'Sec-WebSocket-Protocol' response header. The delegate will be called with a list of the protocols provided
    /// by the client in the 'Sec-WebSocket-Protocol' request header.
    /// </summary>
    /// <remarks>
    /// See RFC 6455 section 1.3 for more details on the WebSocket handshake: https://tools.ietf.org/html/rfc6455#section-1.3
    /// </remarks>
    public Func<IList<string>, string?>? SubProtocolSelector { get; set; }

    public TransferFormat TransferFormat { get; set; } = TransferFormat.Text;
    
    internal bool FramePackets { get; set; }

    /// <summary>
    /// The interval to send keep-alive frames. This is a heart-beat that keeps the connection alive.
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    /// The time to wait for a Pong frame response after sending a Ping frame. If the time is exceeded the websocket will be aborted.
    /// </summary>
    public TimeSpan? KeepAliveTimeout { get; set; }
}