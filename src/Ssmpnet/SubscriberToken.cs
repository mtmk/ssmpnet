using System;
using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    internal class SubscriberToken
    {
        internal Socket Socket;
        internal IPEndPoint EndPoint;
        internal PacketProtocol PacketProtocol;

        internal SubscriberToken(Socket socket)
        {
            Socket = socket;
        }

        internal SubscriberToken(Socket socket, IPEndPoint endPoint)
        {
            Socket = socket;
            EndPoint = endPoint;
        }

        internal Action<byte[]> Receiver { get; set; }
        internal Action Connected;
    }
}