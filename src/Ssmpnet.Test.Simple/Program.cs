using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TAP;

namespace Ssmpnet.LoadTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("[CTRL-C]");
                cancellationTokenSource.Cancel();
            };

            var taskPub = Task.Factory.StartNew(() =>
            {
                var pub = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, 56789));
                Thread.Sleep(1000); // warmup
                int i = 0;
                while (!cancellationToken.IsCancellationRequested && i < 100 * 1000)
                {
                    byte[] message = Encoding.ASCII.GetBytes("Publishing message: " + i++ + new string('x', 1000));
                    pub.Publish(message);
                    //cancellationToken.WaitHandle.WaitOne(10);
                }
                pub.Publish(Encoding.ASCII.GetBytes("END"));
                Thread.Sleep(1000);

                Assert.Ok("Done publishing");

                Assert.Comment("Published {0} messages", i);
            });

            var taskSub = Task.Factory.StartNew(() =>
            {
                var sw = new Stopwatch();

                int i = 0;
                long total = 0;

                SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, 56789),
                    m =>
                    {
                        Interlocked.Increment(ref i);
                        string message = Encoding.ASCII.GetString(m);
                        Interlocked.Add(ref total, m.Length);
                        //Console.WriteLine("Received: {0}", message);
                        if (message == "END")
                        {
                            sw.Stop();
                            Assert.Ok("Received end message");
                            cancellationTokenSource.Cancel();
                        }
                    }, sw.Start);

                cancellationToken.WaitHandle.WaitOne();

                var count = Thread.VolatileRead(ref i);
                var permsg = TimeSpan.FromTicks(sw.Elapsed.Ticks / count);
                var totalBytes = (double)Thread.VolatileRead(ref total);
                var totalMb = totalBytes / (1024 * 1024);
                var mbpersec = totalMb / sw.Elapsed.Seconds;

                Assert.Ok("Done subscribing");

                Assert.Comment("Received {0} ({1:0.00}MB) messages", count, totalMb);
                Assert.Comment("Time: {0} ({1} per msg)", sw.Elapsed, permsg);
                Assert.BenchVar("TIME", permsg, "permsg");
                Assert.BenchVar("TP", mbpersec, "mbpersec");
            });

            Task.WaitAll(taskPub, taskSub);

            Assert.Ok("Finished tests");
        }
    }
}
