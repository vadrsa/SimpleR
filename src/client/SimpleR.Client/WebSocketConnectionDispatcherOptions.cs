using System;
using System.IO.Pipelines;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace SimpleR.Client
{
    public class WebSocketConnectionDispatcherOptions
    {
        // Selected because this is the default value of PipeWriter.PauseWriterThreshold.
        // There maybe the opportunity for performance gains by tuning this default.
        private const int DefaultBufferSize = 65536;

        private PipeOptions _transportPipeOptions;
        private PipeOptions _appPipeOptions;
        private long _transportMaxBufferSize;
        private long _applicationMaxBufferSize;

        public WebSocketConnectionDispatcherOptions()
        {
            WebSockets = new WebsocketClientOptions();
            TransportMaxBufferSize = DefaultBufferSize;
            ApplicationMaxBufferSize = DefaultBufferSize;
        }

        /// <summary>
        /// Gets the <see cref="WebSocketOptions"/> used by the web sockets transport.
        /// </summary>
        public WebsocketClientOptions WebSockets { get; }

        /// <summary>
        /// Gets or sets the maximum buffer size for data read by the application before backpressure is applied.
        /// </summary>
        /// <remarks>
        /// The default value is 65KB.
        /// </remarks>
        public long TransportMaxBufferSize
        {
            get => _transportMaxBufferSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _transportMaxBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum buffer size for data written by the application before backpressure is applied.
        /// </summary>
        /// <remarks>
        /// The default value is 65KB.
        /// </remarks>
        public long ApplicationMaxBufferSize
        {
            get => _applicationMaxBufferSize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _applicationMaxBufferSize = value;
            }
        }

        // We initialize these lazily based on the state of the options specified here.
        // Though these are mutable it's extremely rare that they would be mutated past the
        // call to initialize the routerware.
        internal PipeOptions TransportPipeOptions => _transportPipeOptions ??= new PipeOptions(pauseWriterThreshold: TransportMaxBufferSize, resumeWriterThreshold: TransportMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);

        internal PipeOptions AppPipeOptions => _appPipeOptions ??= new PipeOptions(pauseWriterThreshold: ApplicationMaxBufferSize, resumeWriterThreshold: ApplicationMaxBufferSize / 2, readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false);
    }
}
