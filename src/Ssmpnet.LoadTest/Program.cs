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

            if (args.Length == 1 && args[0] == "pub")
            {
                var rnd = RandomNumberGenerator.Create();
                byte[] rndBuf = new byte[2];

                var pub = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, 56789));
                Thread.Sleep(1000); // warmup
                int i = 0;
                double size = 0;
                while (!cancellationToken.IsCancellationRequested && i < 100*1000)
                {
                    rnd.GetBytes(rndBuf);
                    int n = BitConverter.ToUInt16(rndBuf, 0);
                    byte[] message = Encoding.ASCII.GetBytes("Publishing message: " + i++ + new string('x', n));
                    pub.Publish(message);
                    size += message.Length;
                    //cancellationToken.WaitHandle.WaitOne(10);
                }
                pub.Publish(Encoding.ASCII.GetBytes("END"));
                Thread.Sleep(1000);

                Assert.Ok("Done publishing");
                
                Assert.Comment("Published {0} messages", i);
                Assert.Comment("Size: {0:0.00}MB avg:{1:0.00}KB", size/(1024*1024), (size/i)/1024);
            }

            else if (args.Length == 1 && args[0] == "sub")
            {
                var sw = new Stopwatch();

                int i = 0;
                long total = 0;
                
                SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, 56789),
                    (m,o,c) =>
                    {
                        Interlocked.Increment(ref i);
                        string message = Encoding.ASCII.GetString(m,o,c);
                        Interlocked.Add(ref total, m.Length);
                        //Console.WriteLine("Received: {0}", message);
                        if (message == "END")
                        {
                            sw.Stop();
                            Assert.Ok("Received end message");
                            cancellationTokenSource.Cancel();
                        }
                    }, null, sw.Start);

                cancellationToken.WaitHandle.WaitOne();
                
                var count = Thread.VolatileRead(ref i);
                var permsg = TimeSpan.FromTicks(sw.Elapsed.Ticks/count);
                var totalBytes = (double)Thread.VolatileRead(ref total);
                var totalMb = totalBytes / (1024 * 1024);
                var mbpersec = totalMb/sw.Elapsed.Seconds;

                Assert.Ok("Done subscribing");

                Assert.Comment("Received {0} ({1:0.00}MB) messages", count, totalMb);
                Assert.Comment("Time: {0} ({1} per msg)", sw.Elapsed, permsg);
                Assert.BenchVar("TIME", permsg, "permsg");
                Assert.BenchVar("TP", mbpersec, "mbpersec");
            }

            else
            {
                Plan.Tests(4);
                Plan.BenchName("SSMPNET");
                Task taskPub = Task.Factory.StartNew(()=>Run("pub"));
                Task taskSub = Task.Factory.StartNew(()=>Run("sub"));
                Task.WaitAll(taskPub, taskSub);

                Assert.Ok("Finished tests");
                //cancellationToken.WaitHandle.WaitOne();
                //Process pub = Process.Start(Assembly.GetEntryAssembly().GetName().Name + ".exe", "pub");
                //Process sub = Process.Start(Assembly.GetEntryAssembly().GetName().Name + ".exe", "sub");
            }
        }

        static void Run(string args)
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

            process.WaitForExit();
        }
    }
}
