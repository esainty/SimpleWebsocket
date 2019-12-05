using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SimpleWebsocket {
    public class ActiveConnection {
        private HttpListenerContext context;
        private Task connection;
        private CancellationTokenSource canceller;
        private int frameSize;
        private int id;

        public ActiveConnection(HttpListenerContext context, int id, int frameSize = 125) {
            this.context = context;
            this.canceller = new CancellationTokenSource();
            this.id = id;
            this.frameSize = frameSize;
            this.connection = establishConnectionAsync(canceller.Token);
        }

        private async Task establishConnectionAsync(CancellationToken token) {

            // Get request and response objects from connection context
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Log request to file. 
            Task logger = logRequestAsync(request);

            if (request.IsWebSocketRequest) {
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                WebSocket webSocket = webSocketContext.WebSocket;

                while (webSocket.State == WebSocketState.Open) {
                    // Throw exception if connection has been requested to close.
                    if (token.IsCancellationRequested) {
                        throw new OperationCanceledException("Connection closed on request");
                    }

                    // Wait to receive a message
                    byte[] packet = new byte[frameSize];
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(packet), CancellationToken.None);
                    while (receiveResult.EndOfMessage != true) {
                        byte[] newPacket = new byte[packet.Length + frameSize];
                        Array.Copy(packet, newPacket, packet.Length);
                        receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(newPacket, packet.Length, frameSize), CancellationToken.None);
                        packet = newPacket;
                    }
                    String message = Encoding.UTF8.GetString(packet);
                    //Console.WriteLine("Server received message: {0}", message);
                    Thread.Sleep(1000);
                    byte[] encodedMessage = Encoding.UTF8.GetBytes("Pong");
                    ArraySegment<byte> buffer = new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length);
                    try {
                        await webSocket.SendAsync(encodedMessage, WebSocketMessageType.Text, true, CancellationToken.None);
                        //Console.WriteLine("Server sent message: 'Pong'");
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                        return;
                    }
                }
            } else {
                // If request is not a websocket request, return default landing HTML. 
                string landingPage = "";
                try {
                    landingPage = File.ReadAllText("pages/landingPage.html");
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
                byte[] data = Encoding.UTF8.GetBytes(landingPage);
                response.ContentType = "text/html";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = data.LongLength;
                await response.OutputStream.WriteAsync(data, 0, data.Length);
                response.Close();
            }
            // Wait until request details finish being written to file. 
            await logger;
            throw new OperationCanceledException("Client made http-only request");
        }

        public void endConnection() {
            canceller.Cancel();
        }

        private async Task logRequestAsync(HttpListenerRequest request)
        {
            using (StreamWriter log = File.AppendText("request-log.txt"))
            {
                await log.WriteLineAsync($"Connection ID: {id} | {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond}");
                await log.WriteLineAsync($"URL accessed: {request.Url.ToString()}");
                await log.WriteLineAsync($"Websocket Request: {request.IsWebSocketRequest}");
                await log.WriteLineAsync($"Client Hostname: {request.UserHostName}");
                await log.WriteLineAsync("------------------------\r\n");
            }
        }
    }
}