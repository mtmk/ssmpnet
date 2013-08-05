using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Ssmpnet.Dumb
{
    public class PublisherToken
    {
        const string Tag = "PublisherToken";

        readonly ConcurrentDictionary<Socket, PublisherClientToken> _subs = new ConcurrentDictionary<Socket, PublisherClientToken>();
        
        internal readonly Socket Socket;
        //private PublisherClientToken publisherClientToken;

        internal PublisherToken(Socket socket)
        {
            Socket = socket;
        }

        internal void AddNewSubscriber(Socket socket)
        {
            var publisherClientToken = new PublisherClientToken(socket, this);
            _subs.TryAdd(socket, publisherClientToken);
        }

        internal void RemoveSubscriber(Socket socket)
        {
            PublisherClientToken _;
            _subs.TryRemove(socket, out _);
        }

        public void Publish(byte[] message)
        {
            //publisherClientToken.Send(message);

            var subscribers = _subs.Values;
            Log.Debug(Tag, "Publishing message..#{0}", subscribers.Count);
            byte[] wrapMessage = message;
            foreach (var s in _subs)
            {
                s.Value.Send(wrapMessage);
            }
        }
    }
}