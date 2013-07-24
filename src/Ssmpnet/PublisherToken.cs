using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
            Log.Debug(Tag, "Publishing message..");
            byte[] wrapMessage = PacketProtocol.WrapMessage(message);
            foreach (var s in Subs.Values)
            {
                s.Send(wrapMessage);
            }
        }

       
    }
}