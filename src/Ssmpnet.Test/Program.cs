using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace Ssmpnet.Test
{
    public class Program
    {
        private static void Main(string[] args)
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
                    pub.Publish(Encoding.ASCII.GetBytes("Publishing message:" + i++ + new string('x', 1*1024*1024)));
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

            else if (args.Length == 1 && args[0] == "pub2")
            {
                var pub = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, 56789));
                int i = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    pub.Publish(Encoding.ASCII.GetBytes("Publishing message: " + i++));
                    Thread.Sleep(1000);
                }
            }

            else if (args.Length == 1 && args[0] == "sub2")
            {
                SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, 56789), 
                    m =>
                    {
                        string message = Encoding.ASCII.GetString(m);
                        Console.WriteLine("Received: {0}", message);
                    });
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
            
            else if (args.Length == 1 && args[0] == "test-packet-protocol2")
            {
                TestPacketProtocol2.Iterate(new PacketProtocol(), 1000 * 1000);
            }

            else
            {
                Console.WriteLine("Usage: program.exe <option>");
            }
        }
    }

    internal class TestPacketProtocol2
    {
        public static void Iterate(PacketProtocol pp, int count)
        {
            var largeMessage = Encoding.ASCII.GetBytes(new string('x', 2*1024));
            //var largeMessage = Encoding.ASCII.GetBytes("hi");

            var bufferSize = 1*1024;

            var packetProtocol2 = pp;
            int messageCount = 0;
            var msg = new byte[0];
            packetProtocol2.MessageArrived += (m) =>
                                              {
                                                  ++messageCount;
                                                  msg = m;
                                              };

            var stopwatchTotal = new Stopwatch();
            var stopwatchWrapper = new Stopwatch();
            var stopwatchDecoder = new Stopwatch();

            stopwatchTotal.Start();

            int c = 0;
            for (int i = 0; i < count; i++)
            {
                stopwatchWrapper.Start();
                byte[] wrapMessage = PacketProtocol.WrapMessage(largeMessage);
                stopwatchWrapper.Stop();

                var buffers = GetBuffers(bufferSize, wrapMessage);

                foreach (var buffer in buffers)
                {
                    stopwatchDecoder.Start();
                    packetProtocol2.DataReceived(buffer);
                    stopwatchDecoder.Stop();

//                    if (messageCount != c)
//                    {
//                        c = messageCount;
//                        string s = Encoding.ASCII.GetString(msg);
//                        if (s != Encoding.ASCII.GetString(largeMessage))
//                            throw new Exception("Assert");
//                    }
                }
            }

            stopwatchTotal.Stop();

            Console.WriteLine("Messages received: {0}", messageCount);
            Console.WriteLine("Wrapper time: {0}", stopwatchWrapper.Elapsed);
            Console.WriteLine("Decoder time: {0}", stopwatchDecoder.Elapsed);
            Console.WriteLine("Total time: {0}", stopwatchTotal.Elapsed);
        }

        private static IEnumerable<byte[]> GetBuffers(int bufferSize, byte[] largeMessage)
        {
            int copied = 0;
            while (copied < largeMessage.Length)
            {
                int bufferLeft = largeMessage.Length - copied;
                int lengthToCopy = Math.Min(bufferSize, bufferLeft);

                var buffer = new byte[lengthToCopy];

                Buffer.BlockCopy(largeMessage, copied, buffer, 0, buffer.Length);

                copied += lengthToCopy;

                yield return buffer;
            }
        }
    }
}