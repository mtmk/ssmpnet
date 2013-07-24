﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    public static class SubscriberSocket
    {
        const string Tag = "SubscriberSocket";
        private const int BufferSize = 64 * 1024;

        public static void Start(IPEndPoint endPoint, Action<byte[]> receiver)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var st = new SubscriberToken(socket, endPoint) {Receiver = receiver};
            var e = new SocketAsyncEventArgs { UserToken = st, RemoteEndPoint = endPoint };
            e.Completed += CompletedConnect;

            if (!socket.ConnectAsync(e)) CompletedConnect(null, e);
        }

        static void CompletedConnect(object sender, SocketAsyncEventArgs e)
        {
            var st = (SubscriberToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                var sst = new SubscriberToken(st.Socket, st.EndPoint)
                          {
                              Receiver = st.Receiver,
                              PacketProtocol = new PacketProtocol{MessageArrived = st.Receiver}
                          };
                var se = new SocketAsyncEventArgs { UserToken = sst };
                var buffer = new byte[BufferSize];
                se.SetBuffer(buffer, 0, BufferSize);
                se.Completed += CompletedReceive;
                if (!st.Socket.ReceiveAsync(se)) CompletedReceive(null, se);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedConnect: {0}", e.SocketError);
                Retry(st.Socket, st.EndPoint, st.Receiver);
            }
        }

        private static void Retry(Socket socket, IPEndPoint endPoint, Action<byte[]> receiver)
        {
            Thread.Sleep(3 * 1000);

            Log.Error(Tag, "Retry..");

            Close(socket);
            Start(endPoint, receiver);
        }

        private static void Close(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            try
            {
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Send);
            }
            catch (SocketException e)
            {
                Log.Error(Tag, "socket.Shutdown: {0}", e);
            }
            socket.Close();
        }

        static void CompletedReceive(object sender, SocketAsyncEventArgs e)
        {
            var st = (SubscriberToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                st.PacketProtocol.DataReceived(e.Buffer, 0, e.BytesTransferred);
                e.SetBuffer(0, e.Buffer.Length);
                if (!st.Socket.ReceiveAsync(e)) CompletedReceive(null, e);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedReceive: {0}", e.SocketError);
                Retry(st.Socket, st.EndPoint, st.Receiver);
            }
        }
    }
}