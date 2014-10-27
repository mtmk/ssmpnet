using System;
using System.Threading;
using Xunit;

namespace ProtobufSockets.Tests
{
    public class ApiWithTransportUsageTests
    {
        [Fact]
        public void Subscriber_receives_message()
        {
            var transport = new TransportEventBased();

            var publisher = new PublisherWithTransport(transport);

            var subscriber = new SubscriberWithTransport(transport);

            var r = new ManualResetEvent(false);

            subscriber.Subscribe<Message>(m =>
            {
                Assert.Equal("payload", m.Payload);
                r.Set();
            });

            var message = new Message {Payload = "payload"};
            publisher.Publish(message);

            r.WaitOne();
        }

        [Fact]
        public void Multiple_subscribers_all_receive_message()
        {
            var transport = new TransportEventBased();

            var publisher = new PublisherWithTransport(transport);

            var subscriber1 = new SubscriberWithTransport(transport);
            var subscriber2 = new SubscriberWithTransport(transport);

            var r1 = new ManualResetEvent(false);
            var r2 = new ManualResetEvent(false);

            subscriber1.Subscribe<Message>(m =>
            {
                Assert.Equal("payload", m.Payload);
                r1.Set();
            });

            subscriber2.Subscribe<Message>(m =>
            {
                Assert.Equal("payload", m.Payload);
                r2.Set();
            });

            var message = new Message { Payload = "payload" };
            publisher.Publish(message);

            Assert.True(r1.WaitOne(TimeSpan.FromSeconds(2)));
            Assert.True(r2.WaitOne(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public void Multiple_subscribers_with_different_topics_receive_only_topical_messages()
        {
            var transport = new TransportEventBased();

            var publisher = new PublisherWithTransport(transport);

            var subscriber1 = new SubscriberWithTransport(transport);
            var subscriber2 = new SubscriberWithTransport(transport);

            var r1 = new ManualResetEvent(false);
            var r2 = new ManualResetEvent(false);

            subscriber1.Subscribe<Message>(m =>
            {
                Assert.Equal("payload1", m.Payload);
                r1.Set();
            }, "topic1");

            subscriber2.Subscribe<Message>(m =>
            {
                Assert.Equal("payload2", m.Payload);
                r2.Set();
            }, "topic2");

            publisher.Publish("topic1", new Message { Payload = "payload1" });
            publisher.Publish("topic2", new Message { Payload = "payload2" });

            Assert.True(r1.WaitOne(TimeSpan.FromSeconds(2)));
            Assert.True(r2.WaitOne(TimeSpan.FromSeconds(2)));
        }
    }
}