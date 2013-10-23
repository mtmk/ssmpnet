using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Ssmpnet
{
    public class PublisherToken
    {
        private const string Tag = "PublisherToken";

        private readonly ConcurrentDictionary<Socket, PublisherClientToken> _subs = new ConcurrentDictionary<Socket, PublisherClientToken>();

        internal readonly Socket Socket;

        internal PublisherToken(Socket socket)
        {
            Socket = socket;
        }

        internal void AddNewSubscriber(Subscription sub)
        {
            _subs.TryAdd(sub.Socket, new PublisherClientToken(sub.Socket, this, sub.Topics));
        }

        internal void RemoveSubscriber(Socket socket)
        {
            PublisherClientToken _;
            _subs.TryRemove(socket, out _);
        }

        public void Publish(byte[] message)
        {
            Publish(null, message);
        }

        public void Publish(string topic, byte[] message)
        {
            var subscribers = _subs.Values;
            Log.Debug(Tag, "Publishing message..#{0}", subscribers.Count);


            foreach (var s in _subs)
            {
                try
                {
                    s.Value.Send(topic, message);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}