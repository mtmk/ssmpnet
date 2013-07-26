using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    public class PublisherToken
    {
        const string Tag = "PublisherToken";

        readonly ConcurrentDictionary<Socket, PublisherClientToken> _subs = new ConcurrentDictionary<Socket, PublisherClientToken>();
        
        internal readonly Socket Socket;

        internal PublisherToken(Socket socket)
        {
            Socket = socket;
        }

        internal void AddNewSubscriber(Socket socket)
        {
            _subs.TryAdd(socket, new PublisherClientToken(socket, this));
        }

        internal void RemoveSubscriber(Socket socket)
        {
            PublisherClientToken _;
            _subs.TryRemove(socket, out _);
        }

        public void Publish(byte[] message)
        {
            var subscribers = _subs.Values;
            Log.Debug(Tag, "Publishing message..#{0}", subscribers.Count);
            byte[] wrapMessage = PacketProtocol.WrapMessage(message);
            foreach (var s in subscribers)
            {
                s.Send(wrapMessage);
            }
        }
    }
}