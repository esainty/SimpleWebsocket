using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    public class WebsocketConnection {
        private Task connection;
        WebSocket websocket;
        private CancellationTokenSource canceller;
        private int frameSize;
        private int id;

        public WebsocketConnection(WebSocket websocket, int id, int frameSize = 125) {
            this.canceller = new CancellationTokenSource();
            this.id = id;
            this.frameSize = frameSize;
            this.websocket = websocket;
            this.connection = manageWebsocketAsync(canceller.Token);
        }

        private async Task manageWebsocketAsync(CancellationToken token) {
            while (websocket.State == WebSocketState.Open) {
                // Throw exception if connection has been requested to close.
                if (token.IsCancellationRequested) {
                    throw new OperationCanceledException("Connection closed on request");
                }

                // Wait to receive a message
                byte[] packet = new byte[frameSize];
                WebSocketReceiveResult receiveResult = await websocket.ReceiveAsync(new ArraySegment<byte>(packet), CancellationToken.None);
                while (receiveResult.EndOfMessage != true) {
                    byte[] newPacket = new byte[packet.Length + frameSize];
                    Array.Copy(packet, newPacket, packet.Length);
                    receiveResult = await websocket.ReceiveAsync(new ArraySegment<byte>(newPacket, packet.Length, frameSize), CancellationToken.None);
                    packet = newPacket;
                }
                String message = Encoding.UTF8.GetString(packet);
                //Console.WriteLine("Server received message: {0}", message);
                Thread.Sleep(1000);
                byte[] encodedMessage = Encoding.UTF8.GetBytes("Pong");
                ArraySegment<byte> buffer = new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length);
                try {
                    await websocket.SendAsync(encodedMessage, WebSocketMessageType.Text, true, CancellationToken.None);
                    //Console.WriteLine("Server sent message: 'Pong'");
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                    return;
                }
            }
        }

        public void endConnection() {
            canceller.Cancel();
        }
    }
}