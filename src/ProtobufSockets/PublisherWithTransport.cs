using System;
using System.Collections.Generic;

namespace ProtobufSockets
{
    public class PublisherWithTransport
    {
        private readonly ITransport _transport;

        public PublisherWithTransport(ITransport transport)
        {
            _transport = transport;
        }

        public void Publish<T>(T message)
        {
            Publish(null, message);
        }

        public void Publish<T>(string topic, T message)
        {
            _transport.OnMessageArrived(topic, message);
        }
    }

    public interface ITransport
    {
        void OnMessageArrived(string topic, object message);
        void InterestedIn(string topic, Action<object> receiver);
    }

    public class TransportEventBased : ITransport
    {
        class X
        {
            public event Action<object> MessageArrived;
            public void OnMessageArrived(object message)
            {
                var h = MessageArrived;
                if (h != null)
                    h(message);
            }
        }

        readonly Dictionary<string, X> _xs = new Dictionary<string, X>();

        public void OnMessageArrived(string topic, object message)
        {
            if (topic == null) topic = "__DEFAULT__";

            if (!_xs.ContainsKey(topic)) _xs[topic] = new X();
            _xs[topic].OnMessageArrived(message);
        }

        public void InterestedIn(string topic, Action<object> receiver)
        {
            if (topic == null) topic = "__DEFAULT__";

            if (!_xs.ContainsKey(topic)) _xs[topic] = new X();
            _xs[topic].MessageArrived += receiver;
        }
    }
}
