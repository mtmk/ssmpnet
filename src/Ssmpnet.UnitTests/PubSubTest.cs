using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Ssmpnet.UnitTests
{
    [TestFixture]
    public class PubSubTest
    {
        [Test]
        public void Large_message_consistency()
        {
            const int numMsg = 10;

            var portEvent = new ManualResetEvent(false);
            var startEvent = new ManualResetEvent(false);
            var doneEvent = new ManualResetEvent(false);
            int port = 0;

            var pub = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        PublisherToken p = PublisherSocket.Start(new IPEndPoint(IPAddress.Loopback, 0));

                        Console.WriteLine(p.LocalEndPoint);
                        Thread.VolatileWrite(ref port, p.LocalEndPoint.Port);
                        portEvent.Set();

                        while (!startEvent.WaitOne(100))
                            p.Publish(Encoding.UTF32.GetBytes("START"));

                        for (int i = 0; i < numMsg; i++)
                        {
                            p.Publish(Encoding.UTF32.GetBytes("BEGIN_" + i + new string('x', 2 * 1024 * 1024) + "END_" + i));
                            Thread.Sleep(5);
                        }

                        p.Publish(Encoding.UTF32.GetBytes("DONE"));

                        doneEvent.WaitOne();
                    }
                    finally 
                    {
                        startEvent.Set();
                        doneEvent.Set();
                    }
                });

            var sub = Task.Factory.StartNew(() =>
                {
                    portEvent.WaitOne();
                    int count = 0;
                    SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, Thread.VolatileRead(ref port)), (m, o, c) =>
                        {
                            try
                            {
                                var s = Encoding.UTF32.GetString(m, o, c);

                                if (s == "START")
                                {
                                    startEvent.Set();
                                    return;
                                }

                                if (s == "DONE")
                                {
                                    doneEvent.Set();
                                    return;
                                }

                                Assert.That(s.StartsWith("BEGIN_" + count));
                                Assert.That(s.EndsWith("END_" + count));

                                count++;
                            }
                            catch 
                            {
                                startEvent.Set();
                                doneEvent.Set();
                                throw;
                            }
                        });

                    doneEvent.WaitOne();

                    Assert.That(count == numMsg);
                });

            Task.WaitAll(pub, sub);
        }
    }
}