using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ssmpnet
{
    public static class Server
    {
        const string Tag = "Server";

        public static void Start(IPEndPoint endPoint)
        {
            var acceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var ae = new SocketAsyncEventArgs { UserToken = new UserToken(acceptSocket) };
            ae.Completed += CompletedAccept;

            acceptSocket.Bind(endPoint);
            acceptSocket.Listen(100);
            if (!acceptSocket.AcceptAsync(ae)) CompletedAccept(null, ae);
        }

        static void CompletedAccept(object sender, SocketAsyncEventArgs e)
        {
            var ut = (UserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                Socket socket = e.AcceptSocket;
                e.AcceptSocket = null;
                if (!ut.Socket.AcceptAsync(e)) CompletedAccept(null, e);

                Log.Info(Tag, "Client connected.. [RemoteEndPoint:{0}]", socket.RemoteEndPoint);

                var packetProtocol = new PacketProtocol();
                var userToken = new UserToken(socket)
                {
                    PacketProtocol = packetProtocol
                };

                packetProtocol.MessageArrived += m =>
                {
                    string message = Encoding.ASCII.GetString(m);
                    Log.Info(Tag, "recv: {0}", message);
                };

                var se = new SocketAsyncEventArgs { UserToken = userToken };
                se.Completed += CompletedReceive;
                se.SetBuffer(new byte[1024], 0, 1024);
                if (!socket.ReceiveAsync(se)) CompletedReceive(null, se);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedAccept: {0}", e.SocketError);
            }
        }

        static void CompletedReceive(object sender, SocketAsyncEventArgs e)
        {
            var ut = (UserToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                ut.PacketProtocol.DataReceived(e.Buffer, 0, e.BytesTransferred);
                e.SetBuffer(0, e.Buffer.Length);
                if (!ut.Socket.ReceiveAsync(e)) CompletedReceive(null, e);
            }
            else
            {
                Log.Error(Tag, "Error: CompletedReceive: {0}", e.SocketError);
            }
        }
    }
}