using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    public static class SubscriberSocket
    {
        private static Timer _retry;
        private const string Tag = "SubscriberSocket";
        private const int BufferSize = 64 * 1024;

        public static SubscriberToken Start(IPEndPoint endPoint, Action<byte[]> receiver, Action connected = null)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var st = new SubscriberToken(socket, endPoint, receiver) {Connected = connected};
            var e = new SocketAsyncEventArgs { UserToken = st, RemoteEndPoint = endPoint };
            e.Completed += CompletedConnect;

            if (!socket.ConnectAsync(e)) CompletedConnect(null, e);

            return st;
        }

        private static void CompletedConnect(object sender, SocketAsyncEventArgs e)
        {
            var st = (SubscriberToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                if (st.Connected != null)
                    st.Connected();

                var buffer = new byte[BufferSize];
                e.SetBuffer(buffer, 0, BufferSize);
                e.Completed -= CompletedConnect;
                e.Completed += CompletedReceive;
                Receive(st, e);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedConnect: {0}", e.SocketError);
                Retry(st);
            }
        }

        private static void Retry(SubscriberToken st)
        {
            Log.Info(Tag, "Retry..");
            Close(st.Socket);

            // Try to re-connect in 3 seconds
            if (_retry != null) _retry.Dispose();
            _retry = new Timer(_ => Start(st.EndPoint, st.Receiver, st.Connected), null, 3000, Timeout.Infinite);
        }

        internal static void Close(Socket socket)
        {
            try
            {
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException e)
            {
                Log.Debug(Tag, "socket.Shutdown SocketErrorCode: {0}", e.SocketErrorCode);
            }
            catch (ObjectDisposedException) { }

            socket.Close();
        }

        private static void Receive(SubscriberToken st, SocketAsyncEventArgs e)
        {
            try
            {
                if (!st.Socket.ReceiveAsync(e)) CompletedReceive(null, e);
            }
            catch (ObjectDisposedException) { Retry(st); }
            catch (SocketException) { Retry(st); }
        }

        private static void CompletedReceive(object sender, SocketAsyncEventArgs e)
        {
            var st = (SubscriberToken)e.UserToken;
            
            if (st.CancellationToken.IsCancellationRequested)
                    return;

            if (e.SocketError == SocketError.Success)
            {
                st.Enqueue(e.Buffer, 0, e.BytesTransferred);
                e.SetBuffer(0, e.Buffer.Length);
                Receive(st, e);
            }
            else
            {
                Log.Debug(Tag, "Error: CompletedReceive: {0}", e.SocketError);
                Retry(st);
            }
        }
    }
}