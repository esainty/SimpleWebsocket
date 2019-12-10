using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Net.WebSockets;

namespace SimpleWebsocket {
    public class WebsocketServer {
        private HttpListener listener;
        private int maxConnections;
        private int frameSize;
        private int requestCount;
        private bool serverIsRunning;
        private CancellationTokenSource cts;
        private List<WebsocketConnection> connections;
        private HttpHandler httpHandler;

        public WebsocketServer() {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8000/");
        }

        public Task startServer(int maxConnections = 10, int frameSize = 125) {
            this.maxConnections = maxConnections;
            this.frameSize = frameSize;
            requestCount = 0;
            serverIsRunning = true;
            cts = new CancellationTokenSource();
            connections = new List<WebsocketConnection>();
            httpHandler = new HttpHandler();
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
                    // UNHANDLED TASK - MAY CAUSE THREAD LEAKS
                    Task connectionTask = handleConnection(contextTask);
                } else {
                    if (token.IsCancellationRequested) {
                        foreach (WebsocketConnection connection in connections) {
                            connection.endConnection();
                        }
                        throw new OperationCanceledException("Server successfully shutdown");
                    }
                }
            }
        }
        
        private async Task handleConnection(Task<HttpListenerContext> contextTask) {
            HttpListenerContext context = await contextTask;
            Console.WriteLine("Connection Received");
            
            HttpListenerRequest req = context.Request;
            // Log request to file. 
            Task logger = logRequestAsync(req);      

            if (req.IsWebSocketRequest) {
                if (connections.Count < maxConnections) {
                    // Create websocket connection.
                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                    WebSocket webSocket = webSocketContext.WebSocket;
                    // Create thread for the connection.
                    connections.Add(new WebsocketConnection(webSocket, ++requestCount, frameSize));
                } else {
                    HttpHandler.sendErrorResponse(context.Response, 503);
                }
                
            } else {
                // If request is not a websocket request, handle as HTTP
                HttpListenerResponse res = context.Response;
                await httpHandler.handleHttpRequestAsync(req, res);
            }
            // Wait until request details finish being written to file. 
            await logger;      
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

        private async Task logRequestAsync(HttpListenerRequest request) {
            using (StreamWriter log = File.AppendText("request-log.txt")) {
                await log.WriteLineAsync($"Connection request: {++requestCount} | {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond}");
                await log.WriteLineAsync($"URL accessed: {request.Url.ToString()}");
                await log.WriteLineAsync($"Websocket Request: {request.IsWebSocketRequest}");
                await log.WriteLineAsync($"Client Hostname: {request.UserHostName}");
                await log.WriteLineAsync("------------------------\r\n");
            }
        }
    }
}