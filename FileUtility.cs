using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleWebsocket {

    public static class FileUtility {
        public static string html = "text/html";
        public static string css = "text/css";
        public static string js = "application/javascript";
        public static string json = "application/json";
        public static string jpeg = "image/jpeg";
        public static string png = "image/png";
        public static string gif = "image/gif";
        public static string icon = "image/x-icon";

        private static HashSet<string> _webTypes = new HashSet<string> {".html", ".css", ".js"};
        private static HashSet<string> _imageTypes = new HashSet<string> {".jpg", ".png", ".gif", ".bmp", ".tiff", ".ico"};
        private static HashSet<string> _videoTypes = new HashSet<string> {".mp4", ".avi", ".webm", ".mov", ".flv"};
        private static HashSet<string> _audioTypes = new HashSet<string> {".mp3", ".flac", ".m4a", ".wav"};
        private static HashSet<string> _textTypes = new HashSet<string> {".txt", ".rtf"};

        public static FileType determineFileType(string extension) {
            if (_webTypes.Contains(extension)) {
                switch (extension) {
                    case ".html": return FileType.HTML;
                    case ".css": return FileType.CSS;
                    case ".js": return FileType.JS;
                }
            } else if (_imageTypes.Contains(extension)) {
                return FileType.Image;
            } else if (_audioTypes.Contains(extension)) {
                return FileType.Audio;
            } else if (_videoTypes.Contains(extension)) {
                return FileType.Video;
            } else if (_textTypes.Contains(extension)) {
                return FileType.Text;
            }
            // If no valid return type found, throw exception.
            throw new UriFormatException("Unsupported filetype requested");
        }

        public static bool isResourcePath(string path) {
            Regex resourcePattern = new Regex(@"^[^ ]*\.[a-z]+$");
            return resourcePattern.IsMatch(path) ? true : false;
        }

        public static string getFileExtension(string path) {
            Regex extensionPattern = new Regex(@"\.([a-z0-9]*)$");
            return extensionPattern.Match(path).Value;
        }
    }

    public enum FileType {
        Image,
        Video,
        Audio,
        HTML, 
        CSS, 
        JS, 
        Text,
    }
}