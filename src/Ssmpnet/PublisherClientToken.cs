using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    internal class PublisherClientToken
    {
        private const string Tag = "PublisherClientToken";

        private readonly PublisherToken _parent;
        private readonly SocketAsyncEventArgs _sender;
        private readonly ManualResetEventSlim _r = new ManualResetEventSlim(true);
        private readonly BlockingCollection<byte[]> _q = new BlockingCollection<byte[]>(1000);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        internal Socket Socket;
        internal IPEndPoint EndPoint;
        internal int Count;

        internal PublisherClientToken(Socket socket, PublisherToken publisherToken)
        {
            Socket = socket;
            _sender = new SocketAsyncEventArgs();
            _sender.Completed += CompletedSend;
            _sender.UserToken = this;
            _parent = publisherToken;
            _cancellationToken = _cancellationTokenSource.Token;
            ThreadPool.QueueUserWorkItem(Sender);
        }

        private void Sender(object state)
        {
            try
            {
                foreach (var message in _q.GetConsumingEnumerable(_cancellationToken))
                {
                    SendInternal(message);
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
        }

        internal void Send(byte[] message)
        {
            try
            {
                _q.TryAdd(message, 2, _cancellationToken);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { }
        }

        internal void SendInternal(byte[] message)
        {
            _r.Wait();
            _r.Reset();
            Log.Debug(Tag, "Sending message..");

            _sender.SetBuffer(message, 0, message.Length);
            if (!Socket.SendAsync(_sender)) CompletedSend(null, _sender);
        }

        private static void CompletedSend(object sender, SocketAsyncEventArgs e)
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

        private static void Close(PublisherClientToken pct)
        {
            Log.Debug(Tag, "Closing socket");

            var socket = pct.Socket;
            pct._parent.RemoveSubscriber(socket);
            pct._cancellationTokenSource.Cancel();

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