using System;
using System.IO;
using System.Net;

namespace SimpleWebsocket {
    public class Path {
        public string path;
        public Func<HttpListenerRequest, HttpListenerResponse, int> callback;
        public string contentType;

        public Path(string path, Func<HttpListenerRequest, HttpListenerResponse, int> callback) {
            this.path = path;
            this.callback = callback;
        }

        public Path(string path, string contentType) {
            this.path = path;
            this.contentType = contentType;
        }
    }

    public static class ContentType {
        public static string html = "text/html";
        public static string css = "text/css";
        public static string js = "application/javascript";
        public static string json = "application/json";
        public static string jpeg = "image/jpeg";
        public static string png = "image/png";
        public static string gif = "image/gif";
    }
}