using Min.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Min.httpd
{
    public class WebContainer
    {
        public static readonly object _lock = new object();
        private static WebContainer _container;

        public Context CurrentContext { get; set; }
        public CoreHandler CoreHandler { get; set; }

        private WebContainer()
        {
            CoreHandler = new CoreHandler();
        }

        public static WebContainer Create()
        {
            if (_container == null)
            {
                lock (_lock)
                {
                    if (_container == null)
                    {
                        _container = new WebContainer();
                    }
                }
            }
            return _container;
        }

        public Response Serve(Context context)
        {
            var uri = context.Header["uri"];
            uri = uri.Trim().Replace("\\", "/");
            if (uri.IndexOf('?') >= 0)
            {
                uri = uri.Substring(0, uri.IndexOf('?'));
            }
            if (uri.StartsWith("..") || uri.EndsWith("..") || uri.IndexOf("../") >= 0)
            {
                throw new ServerException(Constants.HTTP_FORBIDDEN, "FORBIDDEN:  \"../\" is forbidden for security reasons.");
            }
            string path;
            if (uri.Equals("/"))
            {
                path = string.Empty;
            }
            else
            {
                path = uri;
            }
            var targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Web" + path);
            Console.WriteLine("target path is: " + targetPath);
            FileInfo file = new FileInfo(targetPath);
            DirectoryInfo directory = new DirectoryInfo(targetPath);
            if (!file.Exists && !directory.Exists)
            {
                string json;
                bool success = CoreHandler.TryGetData(context, out json);
                if (success)
                {
                    return new Response(Constants.HTTP_OK, Constants.MIME_JSON, json);
                }
                else
                {
                    Console.WriteLine($"Error 404 when request {uri}");
                    throw new ServerException(Constants.HTTP_NOTFOUND, "Error 404, not found.");
                }
            }
            else if (directory.Exists)
            {
                if (!uri.EndsWith("/"))
                {
                    path += "/";
                }
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web", path, "index.html");
                var htmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web", path, "index.htm");
                Console.WriteLine("html and htm file path are: " + htmlPath + " , and " + htmPath);
                if (File.Exists(htmlPath))
                {
                    file = new FileInfo(htmlPath);
                }
                else if (File.Exists(htmPath))
                {
                    file = new FileInfo(htmPath);
                }
                else
                {
                    Console.WriteLine($"Error 404 when request {uri}");
                    throw new ServerException(Constants.HTTP_NOTFOUND, "Error 404, not found.");
                }
            }
            var extension = file.Extension.Substring(1).ToLower();
            string mime;
            if (Constants.MimeTypes.Keys.Contains(extension))
            {
                mime = Constants.MimeTypes[extension];
            }
            else
            {
                mime = Constants.MIME_DEFAULT_BINARY;
            }
            long startFrom = 0;
            if (context.Header.Keys.Contains("range"))
            {
                var range = context.Header["range"];
                if (range.StartsWith("bytes="))
                {
                    range = range.Substring("bytes=".Length);
                    int minus = range.IndexOf('-');
                    if (minus > 0)
                    {
                        range = range.Substring(0, minus);
                    }
                    long.TryParse(range, out startFrom);
                }
            }
            FileStream stream = file.OpenRead();
            stream.Position = startFrom;
            var res = new Response(Constants.HTTP_OK, mime, stream);
            res.Header.Add("Content-length", (file.Length - startFrom).ToString());
            res.Header.Add("Content-range", startFrom + "-" + (file.Length - 1) + "/" + file.Length);
            return res;
        }
    }
}
