using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace net_core
{
    class Program
    {
        private static Socket ConnectSocket()
        {
            Socket s = null;
            int port = 9000;
            IPAddress address = IPAddress.Parse("10.10.10.71");
            
            IPEndPoint ipe = new IPEndPoint(address, port);
            Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            tempSocket.Connect(ipe);

            if(tempSocket.Connected)
            {
                s = tempSocket;
               
            }
            else
            {
                
            }
            
            return s;
        }
        static void Main(string[] args)
        {
            
            Byte[] bytesReceived = new Byte[256];
            string receivedString = "";
            using(Socket s = ConnectSocket()) {
                if (s == null){
                    Console.WriteLine ("Connection failed");
                    }
                else{
                    int bytes = 0;
                    do {
                        bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                        receivedString = Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                        Console.WriteLine (receivedString);
                    }
                    while (bytes > 0);
                }

            }
        }
    }
}
