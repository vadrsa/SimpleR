using System;
using System.Net.WebSockets;
using SimpleR.Protocol;

namespace SimpleR.Client
{
    public class SimpleClientBuilder<TMessage>
    {
        private readonly WebSocket _socket;
        private WebSocketConnectionDispatcherOptions? _options;
        private IMessageProtocol<TMessage>? _protocol;

        private SimpleClientBuilder(WebSocket socket)
        {
            _socket = socket;
        }

        public static SimpleClientBuilder<TMessage> Create(WebSocket socket) =>
            new SimpleClientBuilder<TMessage>(socket);

        public SimpleClientBuilder<TMessage> Configure(Action<WebSocketConnectionDispatcherOptions> configure)
        {
            _options = new WebSocketConnectionDispatcherOptions();
            configure(_options);
            return this;
        }
        
        public SimpleClientBuilder<TMessage> WithProtocol(IMessageProtocol<TMessage> protocol)
        {
            _protocol = protocol;
            return this;
        }

        public SimpleClient<TMessage> Build()
        {
            if (_protocol == null)
            {
                throw new InvalidOperationException($"{nameof(WithProtocol)} was not called, but is required.");
            }

            return new SimpleClient<TMessage>(_socket, _protocol, _options ?? new WebSocketConnectionDispatcherOptions());
        }
    }
}