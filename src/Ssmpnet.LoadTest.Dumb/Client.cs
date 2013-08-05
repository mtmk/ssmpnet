using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ssmpnet.LoadTest.Dumb
{
    public static class Client
    {
        const string Tag = "Client";

        public static UserToken Start(IPEndPoint endPoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket.NoDelay = true;
            var userToken = new UserToken(socket, endPoint);

            var e = new SocketAsyncEventArgs { UserToken = userToken, RemoteEndPoint = endPoint };
            e.Completed += CompletedConnect;

            if (!socket.ConnectAsync(e)) CompletedConnect(null, e);

            return userToken;
        }

        static void CompletedConnect(object sender, SocketAsyncEventArgs e)
        {
            var ut = (UserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                var userToken = new UserToken(ut.Socket, ut.EndPoint);
                ut.ClientSocketUserToken = userToken;
                var se = new SocketAsyncEventArgs { UserToken = userToken };
                string message = "count:" + 0.ToString("0000000");
                //Log.Info(Tag, "send: {0}", message);
                var buffer = Encoding.ASCII.GetBytes(message);
                se.SetBuffer(buffer, 0, buffer.Length);
                se.Completed += CompletedSend;
                if (!ut.Socket.SendAsync(se)) CompletedSend(null, se);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedConnect: {0}", e.SocketError);
                Retry(ut.Socket, ut.EndPoint);
            }
        }

        private static void Retry(Socket socket, IPEndPoint endPoint)
        {
            Thread.Sleep(3 * 1000);

            Log.Error(Tag, "Retry..");

            Close(socket);
            Start(endPoint);
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

        static void CompletedSend(object sender, SocketAsyncEventArgs e)
        {
            var ut = (UserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                //Thread.Sleep(1 * 1000);
                int i = Interlocked.Increment(ref ut.Count);
                string message = "count:" + i.ToString("0000000");

                if (i > 1250) return;
                //Log.Info(Tag, "send: {0}", message);
                var buffer = Encoding.ASCII.GetBytes(message);
                e.SetBuffer(buffer, 0, buffer.Length);
                if (!ut.Socket.SendAsync(e)) CompletedSend(null, e);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedSend: {0}", e.SocketError);
                Retry(ut.Socket, ut.EndPoint);
            }
        }
    }
}