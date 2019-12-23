# SimpleWebsocket
Simple multithreaded webserver implementation in C#.NET Core without using ASP.NET. 

**Despite the name, the websocket capabilities of this package have not yet been thoroughly implemented**

## HTTP Implementation

### Create the server

The server will by default run on localhost:8000

```csharp
WebsocketServer server = new WebsocketServer();
Task runningServer = server.startServerAsync();
```

### Allow access to public resources

Any alterations to the server must be made before it is started. 
A public directory can be used to enable files to be accessed directly from http requests. 

```csharp
server.addPublicDirectory(@"resources/public");
```

### Add routes for GET requests

Routes can be added by using ```server.addRoutes(params Tuple<string, Func<HttpListenerRequest, HttpListenerResponse, Task<int>>>[] routes);```
For ease of use and improved readability of code HttpHandler contains a utility function to handle these parameters.

```csharp
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
```

The route executable lambda expression must return whatever HTTP response code it sent to the client.
