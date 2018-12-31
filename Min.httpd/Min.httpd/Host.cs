using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Min.httpd
{
    public class Host : IDisposable
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public int BackLog { get; set; }
        private Socket Listener { get; set; }
        public Host(string ip, int port, int backLog = 100)
        {
            this.IP = ip;
            this.Port = port;
            this.BackLog = backLog;
        }

        public void Start()
        {
            IPAddress address = IPAddress.Parse(IP);
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endpoint = new IPEndPoint(address, Port);
            Listener.Bind(endpoint);
            Listener.Listen(BackLog);
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (true)
                    {
                        Socket serverSocket = Listener.Accept();
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                new SessionHandler(serverSocket).Act();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error: Session act failed.");
                                Console.WriteLine(e);
                                try
                                {
                                    new SessionHandler(serverSocket).ResponseError(e);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error: Report error failed.");
                                    Console.WriteLine(ex);
                                }
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Start listener failed.");
                    Console.WriteLine(e);
                }
            });
        }

        public void Dispose()
        {
            if (Listener != null)
            {
                Listener.Dispose();
            }
        }
    }
}
