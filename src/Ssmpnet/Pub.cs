using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssmpnet
{
    public class Pub
    {
        public void Run(string[] args)
        {
            //T();return;

            var q = new BlockingCollection<byte[]>();
            var sockets = new List<Socket>();

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            socket.Bind(new IPEndPoint(IPAddress.Any, 56789));
            socket.Listen(100);

            EventHandler<SocketAsyncEventArgs> receiveCompleted = (s, e) => { };
            EventHandler<SocketAsyncEventArgs> acceptCompleted
                = (s, e) =>
                      {
                          lock(sockets) sockets.Add(e.AcceptSocket);
                          e.AcceptSocket = null;
                          socket.AcceptAsync(e);
                      };

            var receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += receiveCompleted;

            var acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += acceptCompleted;

            if (!socket.AcceptAsync(acceptArgs))
                acceptCompleted(socket, acceptArgs);

            var task = Task.Factory
                           .StartNew(() =>
                                         {
                                             int i = 0;
                                             while (true)
                                             {
                                                 i++;
                                                 Thread.Sleep(1000);
                                                 List<Socket> list;
                                                 lock (sockets) list = new List<Socket>(sockets);
                                                 foreach (var s in list)
                                                 {
                                                     Socket currentSocket = s;
                                                     if (!currentSocket.Connected)
                                                     {
                                                         Console.WriteLine("Client probably disconnected (socket dead)");
                                                         lock (sockets) sockets.Remove(currentSocket);
                                                         continue;
                                                     }
                                                     var e = new SocketAsyncEventArgs();
                                                     e.Completed += (ss, ee) =>
                                                                        {
                                                                            if (ee.SocketError != SocketError.Success)
                                                                            {
                                                                                Console.WriteLine("Client send error: " + ee.SocketError);
                                                                                lock (sockets) sockets.Remove(currentSocket);
                                                                                CloseSocket(currentSocket);
                                                                            }
                                                                        };
                                                     var msg = "server:count:" + i;

                                                     Console.WriteLine("Sending.. " + msg);

                                                     var buf = Encoding.ASCII.GetBytes(msg);
                                                     buf = PacketProtocol.WrapMessage(buf);
                                                     e.SetBuffer(buf, 0, buf.Length);
                                                     s.SendAsync(e);
                                                 }
                                             }
                                         });

            task.Wait();
        }

        private static void CloseSocket(Socket socket)
        {
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Socket shutdown error: " + exception.GetType());
            }
            socket.Close();
        }

        public void T()
        {
            int numMessages = 0;
            var messages = new string[2];
            var packetizer = new PacketProtocol(2000);
            packetizer.MessageArrived += message =>
            {
                Console.WriteLine("GOT MSG: >>>" + Encoding.UTF8.GetString(message) + "<<<");
                ++numMessages;
            };

            byte[] wrapMessage = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample"));
            int len = wrapMessage.Length;
            int len1 = len/2;
            int len2 = len - len1;

            Console.WriteLine("len1: " + len1);
            Console.WriteLine("len2: " + len2);
            
            var buf1 = new byte[len1];
            var buf2 = new byte[len2];
         
            Buffer.BlockCopy(wrapMessage, 0, buf1, 0, len1);
            Buffer.BlockCopy(wrapMessage, len1, buf2, 0, len2);

            Console.WriteLine("buf1: " + Encoding.ASCII.GetString(buf1, 4, buf1.Length - 4));
            Console.WriteLine("buf2: " + Encoding.ASCII.GetString(buf2));

            packetizer.DataReceived(buf1);
            packetizer.DataReceived(buf2);

            Console.WriteLine("Num messages: {0}", numMessages);
        }
    }
}