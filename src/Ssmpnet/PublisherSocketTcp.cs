using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

//                packetProtocol.MessageArrived += pct.Q.Add;
//
//                var se = new SocketAsyncEventArgs { UserToken = pct };
//                se.Completed += CompletedReceive;
//                se.SetBuffer(new byte[1024], 0, 1024);
//                if (!socket.ReceiveAsync(se)) CompletedReceive(null, se);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedAccept: {0}", e.SocketError);
            }
        }

//        static void CompletedReceive(object sender, SocketAsyncEventArgs e)
//        {
//            var pct = (PublisherClientToken)e.UserToken;
//            if (e.SocketError == SocketError.Success)
//            {
//                pct.PacketProtocol.DataReceived(e.Buffer, 0, e.BytesTransferred);
//                e.SetBuffer(0, e.Buffer.Length);
//                if (!pct.Socket.ReceiveAsync(e)) CompletedReceive(null, e);
//            }
//            else
//            {
//                Log.Error(Tag, "Error: CompletedReceive: {0}", e.SocketError);
//                Close(pct);
//            }
//        }

//        static void Close(PublisherClientToken pct)
//        {
//            var socket = pct.Socket;
//            PublisherClientToken _;
//            pct.Parent.Subs.TryRemove(socket, out _);
//
//            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
//            try
//            {
//                if (socket.Connected)
//                    socket.Shutdown(SocketShutdown.Send);
//            }
//            catch (SocketException e)
//            {
//                Log.Error(Tag, "socket.Shutdown: {0}", e);
//            }
//            socket.Close();
//        }
    }
}