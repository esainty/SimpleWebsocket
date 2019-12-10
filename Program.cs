using System;
using SimpleWebsocket;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    class Program {
        static async Task Main(string[] args) {
            WebsocketServer server = new WebsocketServer();
            Task connection = server.startServer();

            await Task.Delay(2000);
            Client[] clients = new Client[15];
            Task[] instances = new Task[15];
            for (int i = 0; i < 15; i++) {
                clients[i] = new Client();
                instances[i] = clients[i].startClient();
            }

            try {
                await connection;
            } catch (OperationCanceledException) {
                Console.WriteLine("Server instance shutdown");
            }
            
            Console.WriteLine("Program complete");
        }
    }
}
