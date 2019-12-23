using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleWebsocket {

    public static class FileUtility {
        private static Dictionary<string, string> contentLookupTable = new Dictionary<string, string>(){
            {".html", "text/html"},
            {".css", "text/css"},
            {".js", "application/javascript"},
            {".json", "application/json"},
            {".jpg", "image/jpeg"},
            {".png", "image/png"},
            {".gif", "image/gif"},
            {".ico", "image/x-icon"},
        };

        public static Dictionary<ContentType, string> contentTypes = new Dictionary<ContentType, string>(){
            {ContentType.HTML, "text/html"},
            {ContentType.CSS, "text/css"},
            {ContentType.JS, "application/javascript"},
            {ContentType.JSON, "application/json"},
            {ContentType.JPEG, "image/jpeg"},
            {ContentType.PNG, "image/png"},
            {ContentType.GIF, "image/gif"},
            {ContentType.ICON, "image/x-icon"},
        };

        private static HashSet<string> _webTypes = new HashSet<string> {".html", ".css", ".js"};
        private static HashSet<string> _imageTypes = new HashSet<string> {".jpg", ".png", ".gif", ".bmp", ".tiff", ".ico"};
        private static HashSet<string> _videoTypes = new HashSet<string> {".mp4", ".avi", ".webm", ".mov", ".flv"};
        private static HashSet<string> _audioTypes = new HashSet<string> {".mp3", ".flac", ".m4a", ".wav"};
        private static HashSet<string> _textTypes = new HashSet<string> {".txt", ".rtf"};

        public static FileType determineFileType(string extension) {
            if (_webTypes.Contains(extension)) {
                return FileType.Web;
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

        public static string determineContentType(string extension) {
            try {
                return contentLookupTable[extension];
            } catch (Exception) {
                throw new UriFormatException("Unsupported content type requested");
            }
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
        Web,
        Text,
    }

    public enum ContentType {
        HTML, 
        CSS, 
        JS, 
        JSON, 
        JPEG, 
        PNG, 
        GIF, 
        ICON
    }
}