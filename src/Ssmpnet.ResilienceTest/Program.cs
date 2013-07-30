using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Cryptography;
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
                    byte[] message = Encoding.ASCII.GetBytes("Publishing message: " + i++);
                    pub.Publish(message);
                    size += message.Length;
                    cancellationToken.WaitHandle.WaitOne(100);
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
                        string message = Encoding.ASCII.GetString(m);
                        if (!message.StartsWith("Publishing message:"))
                            msgChk = false;
                    }, sw.Start);

                cancellationToken.WaitHandle.WaitOne();

                Assert.Ok("Done subscribing");
                Assert.Ok(i > 10, "Received more than 10 msg - #" + i);
                Assert.Ok(msgChk, "Message check");
            }

            else
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
                //cancellationToken.WaitHandle.WaitOne();
                //Process pub = Process.Start(Assembly.GetEntryAssembly().GetName().Name + ".exe", "pub");
                //Process sub = Process.Start(Assembly.GetEntryAssembly().GetName().Name + ".exe", "sub");
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
