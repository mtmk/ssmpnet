using System;
using System.Net;
using System.Runtime.Remoting.Channels;

namespace ProtobufSockets
{
    public class SubscriberWithTransport
    {
        private readonly ITransport _transport;

        public SubscriberWithTransport(ITransport transport)
        {
            _transport = transport;

        }

        public void Subscribe<T>(Action<T> action)
        {
            Subscribe(action, null);
        }

        public void Subscribe<T>(Action<T> action, string topic)
        {
            _transport.InterestedIn(topic, m => action((T)m));
        }
    }
}