using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ssmpnet
{
    public class Class1
    {
    }

    public class PubSub<T>
    {
        private readonly ConcurrentDictionary<string, Subscriber<T>> _subs = new ConcurrentDictionary<string, Subscriber<T>>();

        public Subscriber<T> Subscribe(string name)
        {
            var subscriber = new Subscriber<T>(name);
            _subs[name] = subscriber;
            return subscriber;
        }

        public void Publish(T message)
        {
            foreach (KeyValuePair<string, Subscriber<T>> kv in _subs)
            {
                kv.Value.Send(message);
            }
        }
    }

    public class Subscriber<T>
    {
        private readonly BlockingCollection<T> _q = new BlockingCollection<T>();

        public Subscriber(string name)
        {
            Name = name;
        }

        public void Send(T message)
        {
            _q.Add(message);
        }

        public string Name { get; private set; }
    }
}
