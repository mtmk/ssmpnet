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
                Assert.Comment("Starting pub");
                var pub = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, 56789));
                Thread.Sleep(1000);
                int i = 0;
                double size = 0;
                byte[] message = Encoding.ASCII.GetBytes("Publishing message: " + i++ + new string('x', 1024 * 1024));
                while (!cancellationToken.IsCancellationRequested)
                {
                    pub.Publish(message);
                    size += message.Length;
                    i++;
                    if (i % 100 == 0) Assert.Comment("Publisher sent {0} messages so far..", i);
                    cancellationToken.WaitHandle.WaitOne(10);
                }

                Assert.Ok("Done publishing");
            });

            var taskSub = Task.Factory.StartNew(() =>
            {
                Assert.Comment("Starting sub");
                var sw = new Stopwatch();

                int i = 0;
                bool msgChk = true;

                SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, 56789),
                    m =>
                    {
                        Interlocked.Increment(ref i);
                        if (i % 100 == 0) Assert.Comment("Subscriber received {0} messages so far..", i);
                        //string message = Encoding.ASCII.GetString(m);
                        //if (!message.StartsWith("Publishing message:"))
                        //    msgChk = false;
                    }, sw.Start);

                cancellationToken.WaitHandle.WaitOne();

                Assert.Ok("Done subscribing");
                Assert.Ok(i > 10, "Received more than 10 msg - #" + i);
                Assert.Ok(msgChk, "Message check");
            });

            Task.WaitAll(taskPub, taskSub);

            Assert.Ok("Finished tests");
        }
    }
}
