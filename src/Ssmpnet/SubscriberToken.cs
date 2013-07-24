using System;
using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    public class SubscriberToken
    {
        public Socket Socket;
        public IPEndPoint EndPoint;
        public int Count;
        public PacketProtocol PacketProtocol;

        public SubscriberToken(Socket socket)
        {
            Socket = socket;
        }

        public SubscriberToken(Socket socket, IPEndPoint endPoint)
        {
            Socket = socket;
            EndPoint = endPoint;
        }

        public Action<byte[]> Receiver { get; set; }
    }
}