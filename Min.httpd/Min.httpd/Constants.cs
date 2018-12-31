using System.Collections.Generic;

namespace Min.httpd
{
    class Constants
    {
        public const string
            HTTP_OK = "200 OK",
            HTTP_REDIRECT = "301 Moved Permanently",
            HTTP_FOUND = "302 Found",
            HTTP_NOTMODIFIED = "304 Not Modified",
            HTTP_FORBIDDEN = "403 Forbidden",
            HTTP_NOTFOUND = "404 Not Found",
            HTTP_BADREQUEST = "400 Bad Request",
            HTTP_INTERNALERROR = "500 Internal Server Error",
            HTTP_NOTIMPLEMENTED = "501 Not Implemented",

            MIME_PLAINTEXT = "text/plain",
            MIME_HTML = "text/html",
            MIME_DEFAULT_BINARY = "application/octet-stream",
            MIME_XML = "text/xml",
            MIME_JSON = "application/json";

        public static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
        {
            { "css", "text/css"},
            { "js", "text/javascript"},
            { "json", "application/json" },
            { "htm", "text/htmlt"},
            { "html", "text/html"},
            { "txt", "text/plain"},
            { "asc", "text/plain"},
            { "mp3", "audio/mpeg"},
            { "m3u", "audio/mpeg-url"},
            { "pdf", "application/pdf"},
            { "doc", "application/msword"},
            { "ogg", "application/x-ogg"},
            { "zip", "application/octet-stream"},
            { "exe", "application/octet-stream"},
            { "class", "application/octet-stream"},
            { "gif", "image/gif"},
            { "jpg", "image/jpeg"},
            { "jpeg", "image/jpeg"},
            { "png", "image/png"},
        };
    }
}
