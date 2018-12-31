using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Min.httpd
{
    public class Response : IDisposable
    {

        public string Status { get; set; }

        public string MimeType { get; set; }

        public Stream Data { get; set; }
        public Dictionary<string, string> Header { get; } = new Dictionary<string, string>();

        public Response()
        {
            this.Status = Constants.HTTP_OK;
        }

        public Response(string status, string mimeType, Stream data)
        {
            this.Status = status;
            this.MimeType = mimeType;
            this.Data = data;
        }

        public Response(string status, string mimeType, string txt)
        {
            this.Status = status;
            this.MimeType = mimeType;
            try
            {
                this.Data = new MemoryStream(Encoding.UTF8.GetBytes(txt));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Dispose()
        {
            if (this.Data != null)
            {
                this.Data.Dispose();
            }
        }
    }
}
