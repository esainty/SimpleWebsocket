using System;
using SimpleWebsocket;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    class Program {
        static async Task Main(string[] args) {
            WebsocketServer server = new WebsocketServer();
            Task connection = server.startServer();
            Client client1 = new Client();
            Client client2 = new Client();

            Task clientInstance1 = client1.startClient();
            Task clientInstance2 = client2.startClient();

            try {
                await connection;
            } catch (OperationCanceledException) {
                Console.WriteLine("Server instance shutdown");
            }
            
            await clientInstance1;
            Console.WriteLine("Program complete");
        }
    }
}
