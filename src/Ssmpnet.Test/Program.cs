using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace Ssmpnet.Test
{
    public class Program
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
                var pub = new PublisherSocketTcpSimple(new Uri("tcp://0.0.0.0:56789"));
                pub.Start(cancellationToken);
                int i = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    pub.Publish(Encoding.ASCII.GetBytes("Publishing message:" + i++ + new string('x', 1 * 1024 * 1024)));
                    //Thread.Sleep(1);
                }
            }

            else if (args.Length == 1 && args[0] == "sub")
            {
                Console.WriteLine();
                Stopwatch sw = Stopwatch.StartNew();
                int count = 1;
                var sub = new SubscriberSocketTcpSimple(new Uri("tcp://localhost:56789"),
                    m =>
                        {
                            string message = Encoding.ASCII.GetString(m);
                            if (count%1000 == 0)
                            {
                                double s = sw.Elapsed.TotalSeconds;
                                double size = (((double) message.Length)/(1024*1024));
                                double mbPerSec = (size*count)/s;
                                
                                Console.CursorLeft = 0;
                                Console.Write("{0}mb - {1:0.00}mb/s {2:0.00}/s", size, mbPerSec, count/s);
                            }
                            count++;
                        });
                sub.Start(cancellationToken);
                cancellationToken.WaitHandle.WaitOne();
            }

            else if (args.Length == 1 && args[0] == "async-cli-serv")
            {
                Server.Start(new IPEndPoint(IPAddress.Any, 12345));
                Client.Start(new IPEndPoint(IPAddress.Loopback, 12345));
                cancellationToken.WaitHandle.WaitOne();
            }

            else if (args.Length == 1 && args[0] == "async-cli")
            {
                Client.Start(new IPEndPoint(IPAddress.Loopback, 12345));
                cancellationToken.WaitHandle.WaitOne();
            }

            else if (args.Length == 1 && args[0] == "async-serv")
            {
                Server.Start(new IPEndPoint(IPAddress.Any, 12345));
                cancellationToken.WaitHandle.WaitOne();
            }

            else
            {
                Console.WriteLine("Usage: program.exe <option>");
            }
        }
    }
}
