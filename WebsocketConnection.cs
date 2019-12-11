using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    public class WebsocketConnection {
        private Task _connection;
        private WebSocket _websocket;
        private CancellationTokenSource _canceller;
        private int _frameSize;
        private int _id;

        public WebsocketConnection(WebSocket websocket, int id, int frameSize = 125) {
            this._canceller = new CancellationTokenSource();
            this._id = id;
            this._frameSize = frameSize;
            this._websocket = websocket;
            this._connection = _manageWebsocketAsync(_canceller.Token);
        }

        private async Task _manageWebsocketAsync(CancellationToken token) {
            while (_websocket.State == WebSocketState.Open) {
                // Throw exception if connection has been requested to close.
                if (token.IsCancellationRequested) {
                    throw new OperationCanceledException("Connection closed on request");
                }

                // Wait to receive a message
                byte[] packet = new byte[_frameSize];
                WebSocketReceiveResult receiveResult = await _websocket.ReceiveAsync(new ArraySegment<byte>(packet), CancellationToken.None);
                while (receiveResult.EndOfMessage != true) {
                    byte[] newPacket = new byte[packet.Length + _frameSize];
                    Array.Copy(packet, newPacket, packet.Length);
                    receiveResult = await _websocket.ReceiveAsync(new ArraySegment<byte>(newPacket, packet.Length, _frameSize), CancellationToken.None);
                    packet = newPacket;
                }
                String message = Encoding.UTF8.GetString(packet);
                //Console.WriteLine("Server received message: {0}", message);
                Thread.Sleep(1000);
                byte[] encodedMessage = Encoding.UTF8.GetBytes("Pong");
                ArraySegment<byte> buffer = new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length);
                try {
                    await _websocket.SendAsync(encodedMessage, WebSocketMessageType.Text, true, CancellationToken.None);
                    //Console.WriteLine("Server sent message: 'Pong'");
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                    return;
                }
            }
        }

        public void endConnection() {
            _canceller.Cancel();
        }
    }
}