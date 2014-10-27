using System;
using System.Net;
using System.Threading;

namespace ProtobufSockets.Tests
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "test")
            {
                new NetworkTests().Publisher_starts_with_an_ephemeral_port();
            }

            const int port = 23456;

            if (args.Length > 0 && args[0] == "sub")
            {
                new Program().Sub(port);
            }

            if (args.Length > 0 && args[0] == "pub1")
            {
                new Program().Pub(port);
            }
            if (args.Length > 0 && args[0] == "pub2")
            {
                new Program().Pub(port + 1);
            }
            if (args.Length > 0 && args[0] == "pub3")
            {
                new Program().Pub(port + 2);
            }
        }

        public void Sub(int port)
        {
            var subscriber = new Subscriber(new[]
            {
                new IPEndPoint(IPAddress.Loopback, port),
                new IPEndPoint(IPAddress.Loopback, port + 1),
                new IPEndPoint(IPAddress.Loopback, port + 2),
            });

            subscriber.Subscribe<Message>("*", m =>
            {
                Console.WriteLine(m.Payload);
            });

            var r = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; r.Set(); };
            r.WaitOne();

            subscriber.Dispose();
        }

        public void Pub(int port)
        {
            var r = new ManualResetEvent(false);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; r.Set(); };

            var publisher = new Publisher(new IPEndPoint(IPAddress.Any, port));
            int i = 0;
            while (true)
            {
                publisher.Publish("*", new Message { Payload = "payload" + i++ });
                if (r.WaitOne(500)) break;
            }

            publisher.Dispose();
        }
    }
}