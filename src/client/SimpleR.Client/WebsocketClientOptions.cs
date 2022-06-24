using System;
using SimpleR.Protocol;

namespace SimpleR.Client
{
    public class WebsocketClientOptions
    {
        /// <summary>
        /// Gets or sets the amount of time the WebSocket transport will wait for a graceful close before starting an ungraceful close.
        /// </summary>
        /// <value>Defaults to 5 seconds</value>
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
        /// <summary>
        /// Sets the bytes that will be set for signaling end of message
        /// </summary>
        internal byte[] EndOfMessageBytes { get; set; } = Array.Empty<byte>();
    
        internal TransferFormat TransferFormat { get; set; }
    }
}
