using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
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
            const int numMsg = 100 * 1000;

            var portEvent = new ManualResetEvent(false);
            var startEvent = new ManualResetEvent(false);
            var doneEvent = new ManualResetEvent(false);
            int port = 0;

            var rnd = RandomNumberGenerator.Create();
            var rndBuf = new byte[2];

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
                        rnd.GetBytes(rndBuf);
                        int n = BitConverter.ToUInt16(rndBuf, 0);
//                        n = 50;
                        p.Publish(Encoding.UTF32.GetBytes("BEGIN_" + i + new string('x', 1 + n) + "END_" + i));
//                        Thread.Sleep(5);
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
                int size = 0;
                var sw = new Stopwatch();
                SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, Thread.VolatileRead(ref port)), (m, o, c) =>
                {
                    try
                    {
                        var s = Encoding.UTF32.GetString(m, o, c);
                        Interlocked.Add(ref size, s.Length);
                        if (s == "START")
                        {
//                            Console.WriteLine("START");
                            startEvent.Set();
                            sw.Start();
                            return;
                        }

                        if (s == "DONE")
                        {
//                            Console.WriteLine("DONE");
                            doneEvent.Set();
                            sw.Stop();
                            return;
                        }

//                        Console.WriteLine("RCV");
                        Assert.That(s.StartsWith("BEGIN_" + count));
                        Assert.That(s.EndsWith("END_" + count));

                        Interlocked.Increment(ref count);
                    }
                    catch
                    {
                        startEvent.Set();
                        doneEvent.Set();
                        throw;
                    }
                });

                doneEvent.WaitOne();

                Assert.That(Thread.VolatileRead(ref count) == numMsg);

                double value = ((double)(Thread.VolatileRead(ref size))) / 1024 / 1024;
                Console.WriteLine("Transferred: {0:0.00} MB", value);
                Console.WriteLine("Elapsed: {0}", sw.Elapsed);
                Console.WriteLine("Transfer rate: {0:0.00} mbits/s", (value * 8) / sw.Elapsed.TotalSeconds);
                Console.WriteLine("Transfer rate: {0:0.00} messages/s", Thread.VolatileRead(ref count) / sw.Elapsed.TotalSeconds);
                Console.WriteLine("Transfer avg size: {0:0.00} MB/per message", value / Thread.VolatileRead(ref count));
            });

            Task.WaitAll(pub, sub);
        }
    }
}