using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace SimpleWebsocket {
    public class Client {

        private ClientWebSocket _cws;
        public string serverURI;

        public Client(string uri = "ws://localhost:8000") {
            _cws = new ClientWebSocket();
            this.serverURI = uri;
        }

        public async Task startClient() {
            Console.WriteLine("Client started, connecting to {0}", "serverURI");
            HttpClient client = new HttpClient();

            try {
                await _cws.ConnectAsync(new Uri(serverURI), CancellationToken.None);
                Console.WriteLine("Connection to server established");
            } catch (WebSocketException e) {
                Console.WriteLine("Connection refused with error: {0}", e.Message);
                return;
            }

            while (_cws.State == WebSocketState.Open) {
                string toEncode = "Ping";
                byte[] encodedMessage = Encoding.UTF8.GetBytes(
                    toEncode);
                ArraySegment<byte> buffer = new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length);
                try {
                    await _cws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    //Console.WriteLine("Client sent message 'Ping'");
                } catch (WebSocketException e) {
                    Console.WriteLine("Message failed to send with error: {0}", e.Message);
                    return;
                }
                byte[] receivedMessage = new byte[1024];
                await _cws.ReceiveAsync(new ArraySegment<byte>(receivedMessage), CancellationToken.None);
                string decodedMessage = Encoding.UTF8.GetString(receivedMessage);
                //Console.WriteLine("Client received message: {0}", decodedMessage);
                Thread.Sleep(1000);
            }
        }
    }
}