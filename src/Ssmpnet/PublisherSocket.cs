using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ssmpnet
{
    public static class PublisherSocket
    {
        private const string Tag = "PublisherSocket";

        public static PublisherToken Start(IPEndPoint endPoint)
        {
            var acceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var publisherToken = new PublisherToken(acceptSocket);

            var ae = new SocketAsyncEventArgs { UserToken = publisherToken };
            ae.Completed += CompletedAccept;

            acceptSocket.Bind(endPoint);

            publisherToken.LocalEndPoint = (IPEndPoint)acceptSocket.LocalEndPoint;

            Log.Info(Tag, "Publisher starting on end point {0}" + publisherToken.LocalEndPoint);

            acceptSocket.Listen(100);
            if (!acceptSocket.AcceptAsync(ae)) ThreadPool.QueueUserWorkItem(_ => CompletedAccept(null, ae));

            return publisherToken;
        }

        private static void CompletedAccept(object sender, SocketAsyncEventArgs e)
        {
            var pt = (PublisherToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                Socket socket = e.AcceptSocket;
                e.AcceptSocket = null;
                if (!pt.Socket.AcceptAsync(e)) CompletedAccept(null, e);

                Log.Info(Tag, "Client connected.. [RemoteEndPoint:{0}]", socket.RemoteEndPoint);

                var re = new SocketAsyncEventArgs { UserToken = new Subscription { Socket = socket, Token = pt } };
                re.Completed += CompletedReceive;
                re.SetBuffer(new byte[512], 0, 512);
                if (!socket.ReceiveAsync(re)) ThreadPool.QueueUserWorkItem(_ => CompletedReceive(null, re));
            }
            else
            {
                Log.Error(Tag, "Error: CompletedAccept: {0}", e.SocketError);
            }
        }

        private static void CompletedReceive(object sender, SocketAsyncEventArgs e)
        {
            var sub = (Subscription)e.UserToken;
            var pt = sub.Token;
            if (e.SocketError == SocketError.Success)
            {
                sub.Topics = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                pt.AddNewSubscriber(sub);
            }
            else
            {
                Log.Debug(Tag, "Error: CompletedReceive: {0}", e.SocketError);
            }
        }
    }

    internal class Subscription
    {
        internal Socket Socket;
        internal string Topics;
        internal PublisherToken Token;
    }
}