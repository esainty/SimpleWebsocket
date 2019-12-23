using System;
using SimpleWebsocket;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace SimpleWebsocket {
    class Program {
        static async Task Main(string[] args) {
            WebsocketServer server = new WebsocketServer();
            server.addPublicDirectory(@"resources/public");
            server.addRoutes(
                HttpHandler.createRoute("/", async (HttpListenerRequest req, HttpListenerResponse res) => {
                    await HttpHandler.sendResponseAsync(res, HttpHandler.prepareWebResponse(res, "resources/public/home.html"));
                    return 200;
                }),
                HttpHandler.createRoute("/spooky", async (HttpListenerRequest req, HttpListenerResponse res) => {
                    await HttpHandler.sendResponseAsync(res, HttpHandler.prepareWebResponse(res, "resources/public/secret.html"));
                    return 200;
                })
            );
            Task connection = server.startServerAsync();

            Client[] clients = new Client[15];
            Task[] instances = new Task[15];
            for (int i = 0; i < 0; i++) {
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
