using Min.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Min.httpd
{
    public class SessionHandler
    {
        public Socket CurrentSocket { get; private set; }

        public SessionHandler(Socket socket)
        {
            this.CurrentSocket = socket;
        }

        public void Act()
        {
            byte[] headerBuffer = new byte[8192];
            int msgLength = CurrentSocket.Receive(headerBuffer, 0, 8192, SocketFlags.None);
            if (msgLength <= 0)
            {
                return;
            }
            var headerStr = Encoding.UTF8.GetString(headerBuffer);
            Dictionary<string, string> header;
            Dictionary<string, string> @params;
            DecodeHeader(headerStr, out header, out @params);
            long restPartSize = long.MaxValue;
            string contentLength = header.ContainsKey("content-length") ? header["content-length"] : string.Empty;
            if (!string.IsNullOrEmpty(contentLength))
            {
                long.TryParse(contentLength, out restPartSize);
            }
            int sepIndex;
            bool sepfound = FindSepLineIndex(headerBuffer, msgLength, out sepIndex);
            sepIndex++;
            byte[] formBuffer;
            formBuffer = GetFormBuffer(headerBuffer, msgLength, restPartSize, sepIndex, sepfound);
            Dictionary<string, string> files = new Dictionary<string, string>();
            var method = header["method"];
            if (method.Equals("POST", StringComparison.CurrentCultureIgnoreCase))
            {
                string contentTypeInfo = header["content-type"];
                var contentTypes = contentTypeInfo.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var contentType = contentTypes.Length > 0 ? contentTypes[0] : string.Empty;
                // application/json
                if (contentType.Equals("application/json", StringComparison.CurrentCultureIgnoreCase))
                {
                    var formContent = Encoding.UTF8.GetString(formBuffer);
                    @params.Add("@json", formContent);
                }
                // application/x-www-form-urlencoded
                else if(contentType.Equals("application/x-www-form-urlencoded", StringComparison.CurrentCultureIgnoreCase))
                {
                    string dataLine = string.Empty;
                    var formContent = Encoding.UTF8.GetString(formBuffer);
                    int endingIndex = formContent.IndexOf("\r\n");
                    dataLine = formContent.Substring(0, endingIndex);
                    dataLine = DecodeEscape(dataLine);
                    DecodeParams(dataLine, ref @params);
                }
                else
                {
                    ResponseError(new Exception("Server don't support the data format of POST request yet."));
                    return;
                }
            }
            var container = WebContainer.Create();
            using (var res = container.Serve(new Context(header, @params, files)))
            {
                SendResponse(res.Status, res.MimeType, res.Header, res.Data);
            }
        }

        private byte[] GetFormBuffer(byte[] headerBuffer, int msgLength, long restPartSize, int sepIndex, bool sepfound)
        {
            byte[] formBuffer;
            using (MemoryStream stream = new MemoryStream())
            {
                if (sepIndex < msgLength)
                {
                    stream.Write(headerBuffer, sepIndex, msgLength - sepIndex);
                    restPartSize -= (msgLength - sepIndex);
                }
                else if (!sepfound || restPartSize == long.MaxValue)
                {
                    restPartSize = 0;
                }
                headerBuffer = new byte[1024];
                while (msgLength >= 0 && restPartSize > 0)
                {
                    msgLength = CurrentSocket.Receive(headerBuffer, 0, 1024, SocketFlags.None);
                    restPartSize -= msgLength;
                    if (msgLength > 0)
                    {
                        stream.Write(headerBuffer, 0, msgLength);
                    }
                }
                formBuffer = stream.ToArray();
            }

            return formBuffer;
        }

        private bool FindSepLineIndex(byte[] headerBuffer, int msgLength, out int sepIndex)
        {
            sepIndex = 0;
            while (sepIndex < msgLength)
            {
                if (headerBuffer[sepIndex] == '\r' && headerBuffer[++sepIndex] == '\n'
                && headerBuffer[++sepIndex] == '\r' && headerBuffer[++sepIndex] == '\n')
                {
                    return true;
                }
                sepIndex++;
            }
            return false;
        }

        public void ResponseError(Exception e)
        {
            string status;
            if (e is ServerException)
            {
                var ex = e as ServerException;
                status = ex.Status;
            }
            else
            {
                status = Constants.HTTP_INTERNALERROR;
            }
            SendResponse(status, Constants.MIME_PLAINTEXT, null, new MemoryStream(Encoding.UTF8.GetBytes(e.Message)));
        }

        private void SendResponse(string status, string mime, Dictionary<string, string> header, Stream data)
        {
            try
            {
                if (header == null)
                {
                    header = new Dictionary<string, string>();
                }
                if (status == null)
                {
                    throw new ServerException(Constants.HTTP_INTERNALERROR, "Response: Status is invalid.");
                }
                StringBuilder headerBuilder = new StringBuilder();
                headerBuilder.AppendLine("HTTP/1.0 " + status);

                if (!header.ContainsKey("Date"))
                { headerBuilder.AppendLine("Date: " + DateTime.UtcNow.ToString("R")); }

                if (mime != null)
                { headerBuilder.AppendLine("Content-Type: " + mime); }

                if (header.Count > 0)
                {
                    foreach (var item in header)
                    {
                        headerBuilder.AppendLine(item.Key + ": " + item.Value);
                    }
                }

                headerBuilder.AppendLine();
                var headerBytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Write(headerBytes, 0, headerBytes.Length);
                    if (data != null)
                    {
                        byte[] buffer = new byte[1024];
                        while (true)
                        {
                            int read = data.Read(buffer, 0, buffer.Length);
                            if (read <= 0)
                            { break; }
                            stream.Write(buffer, 0, read);
                        }
                    }
                    CurrentSocket.Send(stream.ToArray());
                }
                if (data != null)
                {
                    data.Dispose();
                }
                CurrentSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                try
                {
                    CurrentSocket.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        private void DecodeHeader(string headerStr, out Dictionary<string, string> header, out Dictionary<string, string> @params)
        {
            header = new Dictionary<string, string>();
            @params = new Dictionary<string, string>();
            StringReader reader = new StringReader(headerStr);
            {
                var firstLine = reader.ReadLine();
                var splits = firstLine.Split(' ');
                header.Add("method", splits[0]);
                var uri = splits[1];
                Console.WriteLine($"Requesting {uri}");
                uri = DecodeEscape(uri);
                header.Add("uri", uri);
                Console.WriteLine($"after decode escape, uri is {uri}");
                var queryMarkIndex = uri.IndexOf('?');
                if (queryMarkIndex >= 0)
                {
                    DecodeParams(uri.Substring(queryMarkIndex + 1), ref @params);
                }
            }
            var line = reader.ReadLine();
            while (!string.IsNullOrWhiteSpace(line))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var attr = line.Substring(0, colonIndex);
                    var content = line.Substring(colonIndex + 1).Trim();
                    header.Add(attr, content);
                }
                line = reader.ReadLine();
            }
        }

        private void DecodeParams(string queryStr, ref Dictionary<string, string> @params)
        {
            var splits = queryStr.Split('&');
            foreach (var item in splits)
            {
                var equalIndex = item.IndexOf('=');
                if (equalIndex >= 0)
                {
                    var key = item.Substring(0, equalIndex);
                    var value = item.Substring(equalIndex + 1);
                    @params.Add(key, value);
                }
                else
                {
                    @params.Add(item, string.Empty);
                }
            }
        }

        private string DecodeEscape(string encodeStr)
        {
            StringBuilder sb = new StringBuilder();
            var charArray = encodeStr.ToCharArray();
            for (int i = 0; i < charArray.Length; i++)
            {
                var c = charArray[i];
                switch (c)
                {
                    case '+':
                        sb.Append(' ');
                        break;
                    case '%':
                        sb.Append((char)int.Parse(encodeStr.Substring(i + 1, 2), System.Globalization.NumberStyles.HexNumber));
                        i += 2;
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
