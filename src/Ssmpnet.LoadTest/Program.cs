using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                while (!cancellationToken.IsCancellationRequested && i < 1000)
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
                Console.WriteLine("Published {0} messages ({1:0.00}MB avg:{2:0.00}KB)", i, size/(1024*1024), (size/i)/1024);
            }

            else if (args.Length == 1 && args[0] == "sub")
            {
                var sw = new Stopwatch();

                sw.Start();
                int i = 0;
                int total = 0;
                
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
                            cancellationTokenSource.Cancel();
                        }
                    }, sw.Start);

                cancellationToken.WaitHandle.WaitOne();
                TimeSpan permsg = TimeSpan.FromTicks(sw.Elapsed.Ticks/i);

                Console.WriteLine("received {0} ({1:0.00}MB) messages in {2} ({3} per msg)", Thread.VolatileRead(ref i),
                    ((double)Thread.VolatileRead(ref total)) / (1024 * 1024), sw.Elapsed,
                    permsg);
            }

            else
            {
                Task taskPub = Task.Factory.StartNew(()=>Run("PUB", "pub"));
                Task taskSub = Task.Factory.StartNew(()=>Run("SUB", "sub"));
                Task.WaitAll(taskPub, taskSub);
                //cancellationToken.WaitHandle.WaitOne();
                //Process pub = Process.Start(Assembly.GetEntryAssembly().GetName().Name + ".exe", "pub");
                //Process sub = Process.Start(Assembly.GetEntryAssembly().GetName().Name + ".exe", "sub");
            }
        }

        static void Run(string name, string args)
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
                                                  Console.Out.WriteLine("[{0}] OUT {1}", name, data);
                                          };
            process.ErrorDataReceived += (s, e) =>
                                         {
                                             string data = e.Data;
                                             if (data != null)
                                                 Console.Error.WriteLine("[{0}] ERR {1}", name, data);
                                         };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
        }
    }
}
