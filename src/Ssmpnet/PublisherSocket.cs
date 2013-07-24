using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    public static class PublisherSocket
    {
        const string Tag = "PublisherSocket";

        public static PublisherToken Start(IPEndPoint endPoint)
        {
            var acceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var publisherToken = new PublisherToken(acceptSocket);

            var ae = new SocketAsyncEventArgs { UserToken = publisherToken };
            ae.Completed += CompletedAccept;

            acceptSocket.Bind(endPoint);
            acceptSocket.Listen(100);
            if (!acceptSocket.AcceptAsync(ae)) CompletedAccept(null, ae);

            return publisherToken;
        }

        static void CompletedAccept(object sender, SocketAsyncEventArgs e)
        {
            var pt = (PublisherToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                Socket socket = e.AcceptSocket;
                e.AcceptSocket = null;
                if (!pt.Socket.AcceptAsync(e)) CompletedAccept(null, e);

                Log.Info(Tag, "Client connected.. [RemoteEndPoint:{0}]", socket.RemoteEndPoint);

                var packetProtocol = new PacketProtocol();
                var pct = new PublisherClientToken(socket)
                                {
                                    PacketProtocol = packetProtocol
                                };
                pt.Subs.TryAdd(socket, pct);
                pct.Parent = pt;
                pct.Sender.UserToken = pct;
            }
            else
            {
                Log.Error(Tag, "Error: CompletedAccept: {0}", e.SocketError);
            }
        }
    }
}