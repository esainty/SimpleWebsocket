using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    public class WebsocketServer {
        private HttpListener listener;
        private int maxConnections;
        private int frameSize;
        private int requestCount;
        private bool serverIsRunning;
        private CancellationTokenSource cts;
        private List<ActiveConnection> connections;

        public WebsocketServer() {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8000/");   
        }

        public Task startServer(int maxConnections = 10, int frameSize = 125) {
            this.maxConnections = 10;
            this.frameSize = 125;
            requestCount = 0;
            serverIsRunning = true;
            cts = new CancellationTokenSource();
            connections = new List<ActiveConnection>();
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", "http://localhost:8000/");
            Task connectionManager = watchForIncomingConnectionsAsync(cts.Token);
            return connectionManager;
        }

        public void stopServer() {
            listener.Close();
            cts.Cancel();
        }

        private async Task watchForIncomingConnectionsAsync(CancellationToken token) {
            while (serverIsRunning) {
                Console.WriteLine("Waiting for connections");

                // Wait for a connection on a new thread.
                Task<HttpListenerContext> contextTask = listener.GetContextAsync();

                // Empty cancellation token is passed to pollToken
                // There is no reason for it to ever stop polling unless the main token triggers.
                Task pollerTask = pollToken(token, CancellationToken.None);

                // Wait for a connection to trigger or for the server to be shutdown.
                Task triggerTask = await Task.WhenAny(contextTask, pollerTask);

                if (triggerTask == contextTask) {
                    Console.WriteLine("Connection Received");
                    if (connections.Count <= maxConnections) {
                        // Create thread for the connection. Propogate the cancellation token.
                        connections.Add(new ActiveConnection(await contextTask, ++requestCount, frameSize));
                    }
                } else {
                    if (token.IsCancellationRequested) {
                        foreach (ActiveConnection connection in connections) {
                            connection.endConnection();
                        }
                        throw new OperationCanceledException("Server successfully shutdown");
                    }
                }
            }
        }


        private async Task pollToken(CancellationToken token, CancellationToken shouldPoll) {
            while (!shouldPoll.IsCancellationRequested) {
                // Check if server has been closed.
                if (token.IsCancellationRequested) {
                    return;
                }
                else {
                    // Wait 100ms before polling again
                    await Task.Delay(100);
                }
            }
            shouldPoll.ThrowIfCancellationRequested();
        } 
    }
}