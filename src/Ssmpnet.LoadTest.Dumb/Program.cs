using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssmpnet.LoadTest.Dumb
{
    class Program
    {
        const string Tag = "Main";

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
                {
                    Log.Info(Tag, "EXIT: {0}", string.Join(" ", args));
                    cancellationTokenSource.Cancel();
                }
            }, cancellationToken);

            if (args.Length == 1 && args[0] == "server")
            {
                var sw = new Stopwatch();
                int i1 = 100000;
                int total = 0;
                int count = 0;
                Server.Start(new IPEndPoint(IPAddress.Any, 56789), (m, o, c) =>
                                                                   {
                                                                       Interlocked.Add(ref total, c);
                                                                       int inc = Interlocked.Increment(ref count);
                                                                       if (inc == 1) sw.Start();
                                                                       if (inc == i1) sw.Stop();
                                                                       //string message = Encoding.ASCII.GetString(m, o, c);
                                                                       //Log.Info(Tag, "recv: {0} #{1}", message, c);
                                                                   });
                cancellationToken.WaitHandle.WaitOne();
                int t = Thread.VolatileRead(ref total);
                int i = Thread.VolatileRead(ref count);
                Log.Info(Tag, "TOTAL: {0}", t);
                Log.Info(Tag, "COUNT: {0}", i);
                Log.Info(Tag, "i1: {0}", i1);
                Log.Info(Tag, "sw: {0}", sw.Elapsed);
                Log.Info(Tag, "req-per-sec: {0:0.000}", i1/sw.Elapsed.TotalSeconds);
            }

            else if (args.Length == 1 && args[0] == "client")
            {
                Client.Start(new IPEndPoint(IPAddress.Loopback, 56789));
                cancellationToken.WaitHandle.WaitOne();
            }

            else
            {
                var server = Run("server");

                Thread.Sleep(3000);

                var client = Run("client");

                Thread.Sleep(5000);
                
                client.StandardInput.Write("EXIT\n");

                Thread.Sleep(3000);

                server.StandardInput.Write("EXIT\n");


                server.WaitForExit();
                client.WaitForExit();
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
