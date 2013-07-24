using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    public class PublisherClientToken
    {
        const string Tag = "PublisherClientToken";

        public Socket Socket;
        public IPEndPoint EndPoint;
        public int Count;
        public PacketProtocol PacketProtocol;
        public PublisherToken Parent;
        public SocketAsyncEventArgs Sender;

        public PublisherClientToken(Socket socket)
        {
            Socket = socket;
            Sender = new SocketAsyncEventArgs();
            Sender.Completed += CompletedSend;
        }

        public void Send(byte[] message)
        {
            Log.Debug(Tag, "Sending message..");
            Sender.SetBuffer(message, 0, message.Length);
            if (!Socket.SendAsync(Sender)) CompletedSend(null, Sender);
        }

        static void CompletedSend(object sender, SocketAsyncEventArgs e)
        {
            var pct = (PublisherClientToken)e.UserToken;

            Log.Debug(Tag, "Completed send: {0}", e.SocketError);

            if (e.SocketError == SocketError.Success)
            {
            }
            else
            {
                Close(pct);
            }
        }

        static void Close(PublisherClientToken pct)
        {
            Log.Debug(Tag, "Closing socket");

            var socket = pct.Socket;
            PublisherClientToken _;
            pct.Parent.Subs.TryRemove(socket, out _);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            try
            {
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Send);
            }
            catch (SocketException e)
            {
                Log.Debug(Tag, "Error in shutdown: {0}", e.Message);
            }
            socket.Close();
        }
    }
}