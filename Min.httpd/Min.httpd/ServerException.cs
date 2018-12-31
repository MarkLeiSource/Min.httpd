using System;
using System.Collections.Generic;
using System.Text;

namespace Min.httpd
{
    public class ServerException : Exception
    {
        public string Status { get; set; }
        public ServerException(string status, string msg) :
            base(msg)
        {
            Status = status;
        }
    }
}
