using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using TAP;

namespace Ssmpnet.LoadTest.Netmq
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("[CTRL-C]");
                cancellationTokenSource.Cancel();
            };

            Task.Factory.StartNew(() =>
            {
                var c = Console.ReadLine();
                if (c == "EXIT")
                    cancellationTokenSource.Cancel();
            }, cancellationToken);

            if (args.Any(a => a == "pub"))
            {
                var rnd = RandomNumberGenerator.Create();
                byte[] rndBuf = new byte[2];
                Console.WriteLine("Publishing weather updates...");

                using (var context = NetMQContext.Create())
                using (var publisher = context.CreatePublisherSocket())
                {
                    publisher.Bind("tcp://127.0.0.1:5556");

                    Thread.Sleep(1000); // warmup
                    int i = 0;
                    double size = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        rnd.GetBytes(rndBuf);
                        int n = BitConverter.ToUInt16(rndBuf, 0);
                        
                        n = 1000; // XXX netmq seems to have issues with large messages,
                        // or more likely a setting needs to be changed
                        
                        byte[] message = Encoding.ASCII.GetBytes("*Publishing message: " + i++ + new string('x', n));
                        publisher.Send(message);
                        size += message.Length;
                        
                        //cancellationToken.WaitHandle.WaitOne(3000);
                        //Console.WriteLine("ppp");
                    }
                    
                    Assert.Ok("Done publishing");

                    Assert.Comment("Published {0} messages", i);
                    Assert.Comment("Size: {0:0.00}MB avg:{1:0.00}KB", size / (1024 * 1024), (size / i) / 1024);
                }
            }

            else if (args.Any(a => a == "sub"))
            {
                var sw = new Stopwatch();

                sw.Start();
                int i = 0;
                int total = 0;

                using (var context = NetMQContext.Create())
                using (var subscriber = context.CreateSubscriberSocket())
                {
                    subscriber.Connect("tcp://127.0.0.1:5556");
                    subscriber.Subscribe("*");
                    sw.Start();

                    while (!cancellationToken.IsCancellationRequested && i < 100000)
                    {
                        var m = subscriber.Receive();
                        Interlocked.Increment(ref i);
                        Interlocked.Add(ref total, m.Length);
                    }
                }

                //cancellationToken.WaitHandle.WaitOne();
                TimeSpan permsg = TimeSpan.FromTicks(sw.Elapsed.Ticks / i);

                Assert.Ok("Done subscribing");

                Assert.Comment("Received {0} ({1:0.00}MB) messages", Thread.VolatileRead(ref i), ((double)Thread.VolatileRead(ref total)) / (1024 * 1024));
                Assert.Comment("Time: {0} ({1} per msg)", sw.Elapsed, permsg);
                Assert.BenchVar("TIME", permsg, "permsg");
            }

            else
            {
                Plan.Tests(4);
                Plan.BenchName("NETMQ");
                var pub = Run("pub");
                var sub = Run("sub");

                sub.WaitForExit();

                pub.StandardInput.Write("EXIT\n");

                pub.WaitForExit();

                Assert.Ok("Finished tests");
            }
        }

        static Process Run(string args)
        {
            var file = Assembly.GetEntryAssembly().GetName().Name + ".exe";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                }
            };
            process.OutputDataReceived += (s, e) =>
            {
                string data = e.Data;
                if (data != null)
                    Console.Out.WriteLine(data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                string data = e.Data;
                if (data != null)
                    Console.Error.WriteLine(data);
            };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            return process;
        }
    }
}
