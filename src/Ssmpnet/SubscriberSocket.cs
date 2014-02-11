using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ssmpnet
{
    public static class SubscriberSocket
    {
        private const string Tag = "SubscriberSocket";
        private const int BufferSize = 64 * 1024;

        public static SubscriberToken Start(IPEndPoint endPoint, Action<byte[], int, int> receiver, string topics = null, Action connected = null, Config config = null)
        {
            var st = new SubscriberToken(endPoint, receiver)
                {
                    Connected = connected,
                    Topics = topics,
                    Config = config ?? new Config()
                };

            Connect(st);

            return st;
        }

        private static void Connect(SubscriberToken st)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            st.Socket = socket;

            var e = new SocketAsyncEventArgs { UserToken = st, RemoteEndPoint = st.EndPoint };
            e.Completed += CompletedConnect;

            var buf = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(st.Topics) ? "*" : st.Topics);
            e.SetBuffer(buf, 0, buf.Length);

            if (!socket.ConnectAsync(e)) ThreadPool.QueueUserWorkItem(_ => CompletedConnect(null, e));
        }

        private static void CompletedConnect(object sender, SocketAsyncEventArgs e)
        {
            var st = (SubscriberToken)e.UserToken;
            var sct = new SubscriberClientToken(st);

            if (e.SocketError == SocketError.Success)
            {
                if (st.Connected != null)
                    st.Connected();

                var se = new SocketAsyncEventArgs { UserToken = sct }; var buffer = new byte[BufferSize];
                e.SetBuffer(buffer, 0, BufferSize);
                se.SetBuffer(buffer, 0, BufferSize);
                se.Completed += CompletedReceive;
                if (!st.Socket.ReceiveAsync(se)) ThreadPool.QueueUserWorkItem(_ => CompletedReceive(null, se));
            }
            else
            {
                Log.Error(Tag, "Error: CompletedConnect: {0}", e.SocketError);
                Retry(sct);
            }
        }

        internal static void Retry(SubscriberClientToken sct)
        {
            var tryEnter = Monitor.TryEnter(sct);
            if (!tryEnter) return;
            try
            {
                Log.Info(Tag, "Retry..");
                Close(sct.Socket);
                sct.Close();

                // Try to re-connect in 3 seconds
                if (sct.SubscriberToken.Retry != null) sct.SubscriberToken.Retry.Dispose();
                sct.SubscriberToken.Retry = new Timer(_ => Connect(sct.SubscriberToken), null,
                                                      sct.SubscriberToken.Config.ReconnectTimeout, Timeout.Infinite);
            }
            finally
            {
                Monitor.Exit(sct);
            }
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

        private static void Receive(SubscriberClientToken sct, SocketAsyncEventArgs e)
        {
            try
            {
                if (!sct.Socket.ReceiveAsync(e)) ThreadPool.QueueUserWorkItem(_ => CompletedReceive(null, e));
            }
            catch (ObjectDisposedException) { Retry(sct); }
            catch (SocketException) { Retry(sct); }
        }

        private static void CompletedReceive(object sender, SocketAsyncEventArgs e)
        {
            var sct = (SubscriberClientToken)e.UserToken;
            
            if (sct.CancellationToken.IsCancellationRequested)
                    return;

            if (e.SocketError == SocketError.Success)
            {
                sct.Enqueue(e.Buffer, 0, e.BytesTransferred);
                e.SetBuffer(0, e.Buffer.Length);
                Receive(sct, e);
            }
            else
            {
                Log.Debug(Tag, "Error: CompletedReceive: {0}", e.SocketError);
                Retry(sct);
            }
        }
    }
}