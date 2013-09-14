using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TAP;

namespace Ssmpnet.ResilienceTest
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

            Task.Factory.StartNew(() =>
                                  {
                                      var c = Console.ReadLine();
                                      if (c == "EXIT")
                                          cancellationTokenSource.Cancel();
                                  },cancellationToken);

            if (args.Length == 1 && args[0] == "pub")
            {
                Assert.Comment("Starting pub");
                var pub = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, 56789));
                Thread.Sleep(1000);
                int i = 0;
                double size = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    byte[] message = Encoding.ASCII.GetBytes("Publishing message: " + i++ + new string('x', 1024 * 1024));
                    pub.Publish(message);
                    size += message.Length;
                    if (i%1000 == 0) Assert.Comment("Publisher sent {0} messages so far..", i);
                    cancellationToken.WaitHandle.WaitOne(1);
                }

                Assert.Ok("Done publishing");
            }

            else if (args.Length == 1 && args[0] == "sub")
            {
                Assert.Comment("Starting sub");
                var sw = new Stopwatch();

                int i = 0;
                bool msgChk = true;

                SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, 56789),
                    m =>
                    {
                        Interlocked.Increment(ref i);
                        if (i % 1000 == 0) Assert.Comment("Subscriber received {0} messages so far..", i);
                        string message = Encoding.ASCII.GetString(m);
                        if (!message.StartsWith("Publishing message:"))
                            msgChk = false;
                    }, sw.Start);

                cancellationToken.WaitHandle.WaitOne();

                Assert.Ok("Done subscribing");
                Assert.Ok(i > 10, "Received more than 10 msg - #" + i);
                Assert.Ok(msgChk, "Message check");
            }

            else if(args.Length == 1 && args[0] == "multi-sub")
            {
                Plan.Tests(11);

                Process pub = Run("pub");

                Thread.Sleep(2000);

                Process sub1 = Run("sub");
                Process sub2 = Run("sub");
                Process sub3 = Run("sub");
                
                Thread.Sleep(3000);

                sub1.StandardInput.Write("EXIT\n");
                sub2.StandardInput.Write("EXIT\n");
                sub3.StandardInput.Write("EXIT\n");

                Thread.Sleep(2000);
                pub.StandardInput.Write("EXIT\n");

                pub.WaitForExit();
                sub2.WaitForExit();
                sub2.WaitForExit();
                sub3.WaitForExit();

                Assert.Ok("Finished tests");
            }
            
            else if(args.Length == 1 && args[0] == "blink-sub")
            {
                Plan.Tests(92);

                Process pub = Run("pub");

                Thread.Sleep(2000);

                var tasks = new List<Task>();

                for (int i = 0; i < 10; i++)
                    tasks.Add(Task.Factory.StartNew(() =>
                                                    {
                                                        for (int j = 0; j < 3; j++)
                                                        {
                                                            Process sub = Run("sub");
                                                            Thread.Sleep(3000);
                                                            sub.StandardInput.Write("EXIT\n");
                                                            sub.WaitForExit();
                                                        }
                                                    }, cancellationToken));

                Task.WaitAll(tasks.ToArray());

                Thread.Sleep(2000);
                pub.StandardInput.Write("EXIT\n");

                pub.WaitForExit();

                Assert.Ok("Finished tests");
            }
            
            else if(args.Length == 1 && args[0] == "blink-pub")
            {
                Plan.Tests(9);

                Process sub = Run("sub");

                for (int i = 0; i < 5; i++)
                {
                    Process pub = Run("pub");
                    Thread.Sleep(3000);
                    pub.StandardInput.Write("EXIT\n");
                    pub.WaitForExit();
                    Thread.Sleep(1000);
                }

                sub.StandardInput.Write("EXIT\n");
                sub.WaitForExit();

                Assert.Ok("Finished tests");
            }
            
            else if(args.Length == 1 && args[0] == "loop")
            {
                Assert.Comment("Starting loop");

                Process pub = Run("pub");
                Process sub = Run("sub");

                cancellationToken.WaitHandle.WaitOne();

                pub.StandardInput.Write("EXIT\n");
                sub.StandardInput.Write("EXIT\n");

                pub.WaitForExit();
                sub.WaitForExit();

                Assert.Comment("Finished loop");
            }
        }

        static Process Run(string args)
        {
            string name = Assembly.GetEntryAssembly().GetName().Name;
            var file1 = name + ".exe";
            var file = name + "." + args + ".exe";
            File.Copy(file1, file, true);

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
