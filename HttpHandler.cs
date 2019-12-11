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
        public bool allowPublicResources;
        private Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, int>> _paths;

        public HttpHandler(bool allowPublicResources = false) {
            this._paths = new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, int>>();
            this.allowPublicResources = allowPublicResources;
        }

        public void addPath(string path, Func<HttpListenerRequest, HttpListenerResponse, int> lambda) {
            _paths.Add(path, lambda);
        }

        public async Task handleHttpRequestAsync(HttpListenerRequest req, HttpListenerResponse res) {
            string path = req.RawUrl;
            if (_paths.Count == 0) {
                byte[] data = prepareHtmlResponse(res, "pages/landingPage.html");
                await sendResponse(res, data);
            } else if (_paths.ContainsKey(path)) {
                try {
                    int result = _paths[path](req, res);
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            } else if (isResourcePath(path)) {
                if (allowPublicResources) {
                    if (path == "favicon.ico") {
                        prepareImageResponse(res, path);
                    }
                } else {
                    sendErrorResponse(res, 403);
                }
            } else {    
                sendErrorResponse(res, 404);
                throw new HttpListenerException(404, "Unknown URI accessed");
            }
        }

        public static byte[] prepareHtmlResponse(HttpListenerResponse res, string html, bool isPath = true) {
            string htmlstring = "";
            if (isPath) {
                try {
                    htmlstring = File.ReadAllText(html);
                } catch (FileNotFoundException e) {
                    // Pass error up to calling body.
                    throw e;
                }
            } else {
                htmlstring = html;
            }   
            byte[] data = Encoding.UTF8.GetBytes(htmlstring);
            res.ContentType = ContentType.html;
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = data.LongLength;
            return data;
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
            res.ContentType = ContentType.json;
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = data.LongLength;
            return data;
        }

        public static byte[] prepareImageResponse(HttpListenerResponse res, string path) {
            FileStream imagestream;
            try {
                imagestream = new FileStream(path, FileMode.Open, FileAccess.Read);
            } catch (FileNotFoundException e) {
                throw e;
            }
            byte[] data = new byte[imagestream.Length];
            imagestream.Read(data, 0, (int)imagestream.Length);
            imagestream.Close();
            
            string extension = getFileExtension(path);
            switch (extension) {
                case "jpg": 
                    res.ContentType = ContentType.icon;
                    break;
                case "png":
                    res.ContentType = ContentType.png;
                    break;
                case "gif":
                    res.ContentType = ContentType.gif;
                    break;
                case "ico":
                    res.ContentType = ContentType.icon;
                    break;
                default: 
                    throw new FormatException($"File type {path} not accepted.");
            }
            res.ContentLength64 = data.LongLength;

            return data;
        }

        public static bool isResourcePath(string path) {
            Regex resourcePattern = new Regex(@"/^[^ ]*\.[a-z]+$/gi");
            return resourcePattern.IsMatch(path) ? true : false;
        }

        public static string getFileExtension(string path) {
            Regex extensionPattern = new Regex(@"/\.[a-z0-9]*$/gi");
            return extensionPattern.Match(path).Value;
        }

        public static void sendErrorResponse(HttpListenerResponse res, int errorCode) {
            res.StatusCode = errorCode;
            res.Close();
        }

        public static async Task sendResponse(HttpListenerResponse res, byte[] data) {
            await res.OutputStream.WriteAsync(data, 0, data.Length);
            res.Close();
        }
    }
}