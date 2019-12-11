using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebsocket {
    public class HttpHandler {

        private Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, int>> _paths;

        public HttpHandler() {
            _paths = new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, int>>();
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
            } else {
                
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
            res.ContentType = "text/html";
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
            res.ContentType = "application/json";
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = data.LongLength;
            return data;
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