using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ssmpnet
{
    public static class SubscriberSocket
    {
        private static Timer _retry;
        private const string Tag = "SubscriberSocket";
        private const int BufferSize = 64 * 1024;

        public static SubscriberToken Start(IPEndPoint endPoint, Action<byte[], int, int> receiver, string topics = null, Action connected = null)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var st = new SubscriberToken(socket, endPoint, receiver) { Connected = connected, Topics = topics };
            var e = new SocketAsyncEventArgs { UserToken = st, RemoteEndPoint = endPoint };
            var buf = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(topics) ? "*" : topics);
            e.SetBuffer(buf, 0, buf.Length); e.Completed += CompletedConnect;

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

                var sst = new SubscriberToken(st.Socket, st.EndPoint, st.Receiver)
                {
                    PacketProtocol = new PacketProtocol { MessageArrived = st.Receiver }
                };
                var se = new SocketAsyncEventArgs { UserToken = sst }; var buffer = new byte[BufferSize];
                e.SetBuffer(buffer, 0, BufferSize);
                se.SetBuffer(buffer, 0, BufferSize);
                se.Completed += CompletedReceive;
                if (!st.Socket.ReceiveAsync(se)) CompletedReceive(null, se);
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
            _retry = new Timer(_ => Start(st.EndPoint, st.Receiver, st.Topics, st.Connected), null, 3000, Timeout.Infinite);
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