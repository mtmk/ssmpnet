using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    internal class PublisherClientToken
    {
        const string Tag = "PublisherClientToken";

        readonly ManualResetEventSlim _r = new ManualResetEventSlim(true);

        internal Socket Socket;
        internal IPEndPoint EndPoint;
        internal int Count;
        readonly PublisherToken _parent;
        readonly SocketAsyncEventArgs _sender;

        internal PublisherClientToken(Socket socket, PublisherToken publisherToken)
        {
            Socket = socket;
            _sender = new SocketAsyncEventArgs();
            _sender.Completed += CompletedSend;
            _sender.UserToken = this;
            _parent = publisherToken;
        }

        internal void Send(byte[] message)
        {
            _r.Wait();
            _r.Reset();
            Log.Debug(Tag, "Sending message..");

            _sender.SetBuffer(message, 0, message.Length);
            if (!Socket.SendAsync(_sender)) CompletedSend(null, _sender);
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
            pct._r.Set();
        }

        static void Close(PublisherClientToken pct)
        {
            Log.Debug(Tag, "Closing socket");

            var socket = pct.Socket;
            pct._parent.RemoveSubscriber(socket);

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