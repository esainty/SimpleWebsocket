using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    public class HttpHandler {
        private bool _allowPublicResources;
        private string _publicDirectory;
        private Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task<int>>> _routes;

        public HttpHandler(bool allowPublicResources = false) {
            this._routes = new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task<int>>>();
            this._allowPublicResources = allowPublicResources;
        }

        public void addPath(string path, Func<HttpListenerRequest, HttpListenerResponse, Task<int>> lambda) {
            _routes.Add(path, lambda);
        }

        public void addPublicDirectory(string directory) {
            Regex directoryPattern = new Regex(@"^(\/?\w+)+$");
            if (directoryPattern.IsMatch(directory)) {
                _allowPublicResources = true;
                _publicDirectory = directory;
            } else {
                throw new ArgumentException(@"Directory invalid. Must not end with slash.");
            }
        }

        public async Task handleHttpRequestAsync(HttpListenerRequest req, HttpListenerResponse res) {
            string path = req.RawUrl;

            // Checks if specified resource exists and is allowed to be accessed.
            if (FileUtility.isResourcePath(path)) {
                if (_allowPublicResources) {
                    if (File.Exists(_publicDirectory + path)) {
                        FileType filetype = FileUtility.determineFileType(FileUtility.getFileExtension(path));
                        switch (filetype) {
                            case FileType.Web: 
                                byte[] htmlData = prepareWebResponse(res, _publicDirectory + path);
                                await sendResponseAsync(res, htmlData);
                                break;
                            case FileType.Image: 
                                byte[] imageData = prepareImageResponse(res, _publicDirectory + path);
                                await sendResponseAsync(res, imageData);
                                break;
                            default: 
                                // No handler set up for file type. Access denied.
                                await sendErrorResponseAsync(res, 403);
                                break;
                        }
                    } else {
                        // If file cannot be found
                        await sendErrorResponseAsync(res, 404);
                    }
                } else {
                    // If resource access is disallowed
                    await sendErrorResponseAsync(res, 403);
                }

            // If no routes exist return default lander
            } else if (_routes.Count == 0) {
                if (File.Exists(_publicDirectory + "/index.html")) {
                    byte[] data = prepareWebResponse(res, _publicDirectory + "/index.html");
                    await sendResponseAsync(res, data);
                } else {
                    await sendErrorResponseAsync(res, 403);
                }

            } else if (_routes.ContainsKey(path)) {
                // Check if list of routes contains the specified path
                try {
                    int result = await _routes[path](req, res);
                } catch (Exception e) {
                    Console.WriteLine(e);
                }

            } else {    
                // If route cannot be found, return 404
                await sendErrorResponseAsync(res, 404);
            }
        }

        public static byte[] prepareWebResponse(HttpListenerResponse res, string path) {
            try {
                // Get requested file. 
                // Block throw FileNotFoundException if it fails
                string htmlstring = File.ReadAllText(path);
                byte[] data = Encoding.UTF8.GetBytes(htmlstring);

                // Prepare file to be returned to client
                // Block will throw UriFormatException if requested content type is not supported
                string extension = FileUtility.getFileExtension(path);
                res.ContentType = FileUtility.determineContentType(extension);
                res.ContentEncoding = Encoding.UTF8;
                res.ContentLength64 = data.LongLength;
                return data;

            // Pass errors up to calling party to handle
            } catch (UriFormatException e) {
                throw e;
            } catch (FileNotFoundException e) {
                throw e;
            }
        }

        public static byte[] prepareJsonResponse(HttpListenerResponse res, string json, bool isPath = true) {
            string jsonstring = "";
            if (isPath) {
                try {
                    jsonstring = File.ReadAllText(json);
                } catch (FileNotFoundException e) {
                    // Pass error up to calling body.
                    throw e;
                }
            } else {
                jsonstring = json;
            }  
            byte[] data = Encoding.UTF8.GetBytes(jsonstring);
            res.ContentType = FileUtility.contentTypes[ContentType.JSON];
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = data.LongLength;
            return data;
        }

        public static byte[] prepareImageResponse(HttpListenerResponse res, string path) {
            FileStream imagestream;
            try {
                // Get requested file
                // Block will throw FileNotFoundException if it fails
                imagestream = new FileStream(path, FileMode.Open, FileAccess.Read);
                byte[] data = new byte[imagestream.Length];
                imagestream.Read(data, 0, (int)imagestream.Length);
                imagestream.Close();
                
                // Prepare file to be returned to client. 
                // Block will throw UriFormatException if content type is not supported.
                string extension = FileUtility.getFileExtension(path);
                res.ContentType = FileUtility.determineContentType(extension);
                res.ContentLength64 = data.LongLength;
                return data;

            // Pass exceptions up to calling party to handle
            } catch (FileNotFoundException e) {
                throw e;
            } catch (UriFormatException e) {
                throw e;
            }
        }

        public static Tuple<string, Func<HttpListenerRequest, HttpListenerResponse, Task<int>>> createRoute(string route, Func<HttpListenerRequest, HttpListenerResponse, Task<int>> lambda) {
            return new Tuple<string, Func<HttpListenerRequest, HttpListenerResponse, Task<int>>>(route, lambda);
        }

        public static async Task sendErrorResponseAsync(HttpListenerResponse res, int errorCode, byte[] data = null) {
            if (data != null) {
                await res.OutputStream.WriteAsync(data, 0, data.Length);
            }
            res.StatusCode = errorCode;
            res.Close();
        }

        public static async Task sendResponseAsync(HttpListenerResponse res, byte[] data) {
            await res.OutputStream.WriteAsync(data, 0, data.Length);
            res.Close();
        }
    }
}