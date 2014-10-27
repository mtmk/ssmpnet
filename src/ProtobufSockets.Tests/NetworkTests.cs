using System;
using System.Net;
using System.Threading;
using Xunit;

namespace ProtobufSockets.Tests
{
    public class NetworkTests
    {
        [Fact]
        public void Publisher_starts_with_an_ephemeral_port()
        {
            var r = new ManualResetEvent(false);

            var publisher = new Publisher();
            
            var subscriber = new Subscriber(new []{publisher.EndPoint});

            subscriber.Subscribe<Message>("*", m =>
            {
                r.Set();
                Assert.Equal("payload1", m.Payload);
            });

            publisher.Publish("*", new Message {Payload = "payload1"});

            Assert.True(r.WaitOne(3000), "Timed out");

            publisher.Dispose();
            subscriber.Dispose();
        }

        [Fact]
        public void Publish_different_topics()
        {
            var r1 = new ManualResetEvent(false);
            var r2 = new ManualResetEvent(false);

            var publisher = new Publisher();

            var subscriber1 = new Subscriber(new[] { publisher.EndPoint });
            subscriber1.Subscribe<Message>("topic1", m =>
            {
                r1.Set();
                Assert.Equal("payload1", m.Payload);
            });

            var subscriber2 = new Subscriber(new[] { publisher.EndPoint });
            subscriber2.Subscribe<Message>("topic2", m =>
            {
                r2.Set();
                Assert.Equal("payload2", m.Payload);
            });

            publisher.Publish("topic1", new Message { Payload = "payload1" });
            publisher.Publish("topic2", new Message { Payload = "payload2" });

            Assert.True(r1.WaitOne(3000), "Timed out");
            Assert.True(r2.WaitOne(3000), "Timed out");

            publisher.Dispose();
            subscriber1.Dispose();
            subscriber2.Dispose();
        }

        [Fact]
        public void Multiple_publishers_and_subscribers()
        {
            var r1 = new ManualResetEvent(false);
            var r2 = new ManualResetEvent(false);

            var publisher1 = new Publisher();
            var publisher2 = new Publisher();
            var publisher3 = new Publisher();

            var endPoints = new[] { publisher1.EndPoint, publisher2.EndPoint, publisher3.EndPoint };

            var subscriber1 = new Subscriber(endPoints);
            subscriber1.Subscribe<Message>("topic1", m =>
            {
                r1.Set();
                Assert.Equal("payload1", m.Payload);
            });

            var subscriber2 = new Subscriber(endPoints);
            subscriber2.Subscribe<Message>("topic2", m =>
            {
                r2.Set();
                Assert.Equal("payload2", m.Payload);
            });

            publisher1.Publish("topic1", new Message { Payload = "payload1" });
            publisher2.Publish("topic2", new Message { Payload = "payload2" });
            publisher3.Publish("topic2", new Message { Payload = "payload2" });

            Assert.True(r1.WaitOne(3000), "Timed out");
            Assert.True(r2.WaitOne(3000), "Timed out");

            publisher1.Dispose();
            publisher2.Dispose();
            publisher3.Dispose();
            subscriber1.Dispose();
            subscriber2.Dispose();
        }


    }
}