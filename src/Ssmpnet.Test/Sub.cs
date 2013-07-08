using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ssmpnet.Test
{
    public class Sub
    {
        public void Run(string[] args)
        {
            var q = new BlockingCollection<byte[]>();
         var packetProtocol = new PacketProtocol(1024);
            packetProtocol.MessageArrived += m => { q.Add(m); };
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectArgs = new SocketAsyncEventArgs { UserToken = socket };
            var receiveArgs = new SocketAsyncEventArgs();
            connectArgs.Completed += (s, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    Console.WriteLine("Connected " + e.SocketError);
                    receiveArgs.UserToken = e.UserToken;
                    ((Socket)e.UserToken).ReceiveAsync(receiveArgs);
                }
                else
                {
                    Console.WriteLine("Connect: Socket error " + e.SocketError);
                    Thread.Sleep(1000);
                    Console.WriteLine("Retrying..");
                    ReConnect(connectArgs);
                }
            };
            connectArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 56789);
            socket.ConnectAsync(connectArgs);

   

            EventHandler<SocketAsyncEventArgs> receiveCompleted
                = (s, e) =>
                      {
                          var currentSocket = (Socket)e.UserToken;
                          if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                          {
                              var receiveBuffer = new byte[e.BytesTransferred];
                              Buffer.BlockCopy(e.Buffer, 0, receiveBuffer, 0, e.BytesTransferred);
                              packetProtocol.DataReceived(receiveBuffer);
                              e.SetBuffer(0, e.Buffer.Length);
                              currentSocket.ReceiveAsync(e);
                          }
                          else
                          {
                              Console.WriteLine("Receive: Socket error " + e.SocketError);
                              ReConnect(connectArgs);
                          }
                      };

            receiveArgs.Completed += receiveCompleted;
            var buffer = new byte[10];
            receiveArgs.SetBuffer(buffer, 0, buffer.Length);

            Console.WriteLine("Waiting for mesages..");



            foreach (var bytese in q.GetConsumingEnumerable())
            {
                Console.WriteLine("  DEQ: >>> " + Encoding.ASCII.GetString(bytese) + "<<<");
            }
        }

        private static void ReConnect(SocketAsyncEventArgs connectArgs)
        {
            var currentSocket = (Socket)connectArgs.UserToken;
            try
            {
                currentSocket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Sock shut error: " + exception.GetType());
            }
            currentSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            currentSocket.Close();

            connectArgs.UserToken = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ((Socket)connectArgs.UserToken).ConnectAsync(connectArgs);
        }
    }

    public class Framer
    {
        //
        // Jon Cole
        // Simple Message Framing Sample for TCP Socket
        // http://blogs.msdn.com/b/joncole/archive/2006/03/20/simple-message-framing-sample-for-tcp-socket.aspx
        //

        public static byte[] CalculateMessageSize(byte[] data)
        {
            byte[] sizeinfo = new byte[4];

            //could optionally call BitConverter.GetBytes(data.length);
            sizeinfo[0] = (byte)data.Length;
            sizeinfo[1] = (byte)(data.Length >> 8);
            sizeinfo[2] = (byte)(data.Length >> 16);
            sizeinfo[3] = (byte)(data.Length >> 24);

            return sizeinfo;
        }

        public static int GetMessageSize(byte[] sizeinfo)
        {
            int size = 0;

            //could optionally call BitConverter.ToInt32(sizeinfo, 0);
            size |= sizeinfo[0];
            size |= (sizeinfo[1] << 8);
            size |= (sizeinfo[2] << 16);
            size |= (sizeinfo[3] << 24);

            return size;
        }

        private int totalread;
        private int currentread;
        private int state;
        private byte[] msg = null;
        byte[] sizeinfo = new byte[4];
        int size;
        //note, this function does not handle closed connections in the middle of a message...
        public void ReadMessage(byte[] buf, Action<byte[]> received)
        {
            if (state == 0 || state == 1) // Read Size
            {
                //read the size of the message
                if (state == 0)
                {
                    totalread = 0;
                    currentread = 0;
                }

                int len;
                if (state == 0 && buf.Length >= sizeinfo.Length)
                {
                    state = 2;
                    len = sizeinfo.Length;

                }
                else
                {
                    state = 1;
                    len = buf.Length;
                }

                Buffer.BlockCopy(buf, totalread, sizeinfo, totalread, len);
                currentread = len;
                totalread += currentread;
            }

            if (state == 2) // read message
            {
                size = 0;

                //could optionally call BitConverter.ToInt32(sizeinfo, 0);
                size |= sizeinfo[0];
                size |= (sizeinfo[1] << 8);
                size |= (sizeinfo[2] << 16);
                size |= (sizeinfo[3] << 24);
                msg = new byte[size];

                if (buf.Length > totalread)
                {

                }
            }
        }
    }
}