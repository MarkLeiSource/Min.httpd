using System;
using System.Collections.Generic;

namespace Min.httpd
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine("input the IP:");
            string ip = Console.ReadLine().Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = "127.0.0.1";
            }
            Console.WriteLine("input the port:");
            var portString = Console.ReadLine().Trim();
            int port = string.IsNullOrWhiteSpace(portString) ?
                80 : int.Parse(portString);
            using (var host = new Host(ip, port))
            {
                host.Start();
                Console.WriteLine($"Server started... {ip}:{port}");
                string cmd;
                do
                {
                    cmd = Console.ReadLine();
                }
                while (cmd != "exit");
            }
        }
    }
}
