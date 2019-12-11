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
        private HttpListener _listener;
        private int _maxConnections;
        private int _frameSize;
        private int _requestCount;
        private bool _serverIsRunning;
        private CancellationTokenSource _cts;
        private List<WebsocketConnection> _connections;
        private HttpHandler _httpHandler;

        public WebsocketServer() {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8000/");
            _cts = new CancellationTokenSource();
            _connections = new List<WebsocketConnection>();
            _httpHandler = new HttpHandler();
        }

        public Task startServerAsync(int maxConnections = 10, int frameSize = 125) {
            this._maxConnections = maxConnections;
            this._frameSize = frameSize;
            this._requestCount = 0;
            this._serverIsRunning = true;
            _listener.Start();
            Console.WriteLine("Listening for connections on {0}", "http://localhost:8000/");
            Task connectionManager = _watchForIncomingConnectionsAsync(_cts.Token);
            return connectionManager;
        }

        public void stopServer() {
            _listener.Close();
            _cts.Cancel();
        }

        public void addPaths(params Tuple<string, Func<HttpListenerRequest, HttpListenerResponse, int>>[] paths) {
            for (int i = 0; i < paths.Length; i++) {
                _httpHandler.addPath(paths[i].Item1, paths[i].Item2);
            }
        }

        private async Task _watchForIncomingConnectionsAsync(CancellationToken token) {
            while (_serverIsRunning) {
                Console.WriteLine("Waiting for connections");

                // Wait for a connection on a new thread.
                Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

                // Empty cancellation token is passed to pollToken
                // There is no reason for it to ever stop polling unless the main token triggers.
                Task pollerTask = _pollTokenAsync(token, CancellationToken.None);

                // Wait for a connection to trigger or for the server to be shutdown.
                Task triggerTask = await Task.WhenAny(contextTask, pollerTask);
                
                if (triggerTask == contextTask) {
                    // UNHANDLED TASK - MAY CAUSE THREAD LEAKS
                    Task connectionTask = _handleConnectionAsync(contextTask);
                } else {
                    if (token.IsCancellationRequested) {
                        foreach (WebsocketConnection connection in _connections) {
                            connection.endConnection();
                        }
                        throw new OperationCanceledException("Server successfully shutdown");
                    }
                }
            }
        }
        
        private async Task _handleConnectionAsync(Task<HttpListenerContext> contextTask) {
            HttpListenerContext context = await contextTask;
            Console.WriteLine("Connection Received");
            
            HttpListenerRequest req = context.Request;
            // Log request to file. 
            Task logger = _logRequestAsync(req);      

            if (req.IsWebSocketRequest) {
                if (_connections.Count < _maxConnections) {
                    // Create websocket connection.
                    HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                    WebSocket webSocket = webSocketContext.WebSocket;
                    // Create thread for the connection.
                    _connections.Add(new WebsocketConnection(webSocket, ++_requestCount, _frameSize));
                } else {
                    HttpHandler.sendErrorResponse(context.Response, 503);
                }
                
            } else {
                // If request is not a websocket request, handle as HTTP
                HttpListenerResponse res = context.Response;
                await _httpHandler.handleHttpRequestAsync(req, res);
            }
            // Wait until request details finish being written to file. 
            await logger;      
        }

        private async Task _pollTokenAsync(CancellationToken token, CancellationToken shouldPoll) {
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

        private async Task _logRequestAsync(HttpListenerRequest request) {
            using (StreamWriter log = File.AppendText("request-log.txt")) {
                await log.WriteLineAsync($"Connection request: {++_requestCount} | {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond}");
                await log.WriteLineAsync($"URL accessed: {request.Url.ToString()}");
                await log.WriteLineAsync($"Websocket Request: {request.IsWebSocketRequest}");
                await log.WriteLineAsync($"Client Hostname: {request.UserHostName}");
                await log.WriteLineAsync("------------------------\r\n");
            }
        }
    }
}