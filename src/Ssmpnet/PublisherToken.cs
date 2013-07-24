using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    public class PublisherToken
    {
        const string Tag = "PublisherToken";

        public Socket Socket;
        public IPEndPoint EndPoint;
        public int Count;
        public PacketProtocol PacketProtocol;
        public ConcurrentDictionary<Socket, PublisherClientToken> Subs = new ConcurrentDictionary<Socket, PublisherClientToken>(); 

        public PublisherToken(Socket socket)
        {
            Socket = socket;
        }

        public PublisherToken(Socket socket, IPEndPoint endPoint)
        {
            Socket = socket;
            EndPoint = endPoint;
        }

        public void Publish(byte[] message)
        {
            var subscribers = Subs.Values;
            Log.Debug(Tag, "Publishing message..#{0}", subscribers.Count);
            byte[] wrapMessage = PacketProtocol.WrapMessage(message);
            foreach (var s in subscribers)
            {
                s.Send(wrapMessage);
            }
        }
    }
}