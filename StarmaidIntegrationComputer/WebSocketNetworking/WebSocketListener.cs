using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;

namespace StarmaidIntegrationComputer.WebSocketNetworking
{
    internal class WebSocketListener : IDisposable
    {
        private bool isRunning = false;

        private HttpListener? listener;
        private readonly WebSocketSettings settings;
        private readonly ILogger<WebSocketListener> logger;
        private CancellationTokenSource? cancellationTokenSource;

        private List<IDisposable> disposables { get; } = new List<IDisposable>();

        public WebSocketListener(WebSocketSettings settings, LoggerFactory loggerFactory)
        {
            this.settings = settings;

            this.logger = loggerFactory.CreateLogger<WebSocketListener>();
        }

        public async Task Start()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;


            this.listener = new HttpListener();
            disposables.Add(listener);


            this.listener.Prefixes.Add($"${settings.IpAddress}:{settings.Port}");

            listener.Start();

            logger.LogInformation($"Starting to listen for WebSocket connections at {settings.IpAddress}:${settings.Port}");

            HttpListenerContext listenerContext = await listener.GetContextAsync();

            if (listenerContext != null && listenerContext.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext webSocketContext = await listenerContext.AcceptWebSocketAsync(null, 100, TimeSpan.FromSeconds(5));

                WebSocket webSocket = webSocketContext.WebSocket;
                disposables.Add(webSocket);

                cancellationTokenSource = new CancellationTokenSource();


                while (webSocket.State == WebSocketState.Open && cancellationTokenSource?.IsCancellationRequested == false)
                {
                    ArraySegment<byte> receivedBytes = new ArraySegment<byte>();

                    //Discarding result here
                    await webSocket.ReceiveAsync(receivedBytes, cancellationTokenSource.Token);

                    if (receivedBytes.Count == 0 || receivedBytes.Array == null)
                    {
                        continue;
                    }

                    string result = Encoding.UTF8.GetString(receivedBytes.Array);

                    InterpretCommand(result);
                }
            }
        }

        private void InterpretCommand(string result)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            //TODO: Consider if this is the best place to do this! Maybe we should wait until we have observed a cancellation complete. Not yet sure the best way to facilitate that.
            isRunning = false;

        }

        public void Dispose()
        {
            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
