using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace SimpleWebsocket {
    public class Client {

        ClientWebSocket cws;
        string serverURI;

        public Client(string uri = "ws://localhost:8000") {
            cws = new ClientWebSocket();
            this.serverURI = uri;
        }

        public async Task startClient() {
            Console.WriteLine("Client started, connecting to {0}", "serverURI");
            HttpClient client = new HttpClient();
            // try {
            //     HttpResponseMessage message = await client.GetAsync(serverURI);
            //     string content = await message.Content.ReadAsStringAsync();
            //     Console.WriteLine("Message: {0}", content);
            // } catch (Exception e) {
            //     Console.WriteLine(e);
            // }
            try {
                await cws.ConnectAsync(new Uri(serverURI), CancellationToken.None);
                Console.WriteLine("Client state is: {0}", cws.State);
            } catch (WebSocketException e) {
                Console.WriteLine("Connection refused with error: {0}", e.Message);
                return;
            }
            while (cws.State == WebSocketState.Open) {
                string toEncode = "Ping";
                // for (int i = 0; i < 256; i++) {
                //     toEncode += "A";
                // }
                byte[] encodedMessage = Encoding.UTF8.GetBytes(
                    toEncode);
                ArraySegment<byte> buffer = new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length);
                try {
                    await cws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    //Console.WriteLine("Client sent message 'Ping'");
                } catch (WebSocketException e) {
                    Console.WriteLine("Message failed to send with error: {0}", e.Message);
                    return;
                }
                byte[] receivedMessage = new byte[1024];
                await cws.ReceiveAsync(new ArraySegment<byte>(receivedMessage), CancellationToken.None);
                string decodedMessage = Encoding.UTF8.GetString(receivedMessage);
                //Console.WriteLine("Client received message: {0}", decodedMessage);
                Thread.Sleep(1000);
            }
        }
    }
}