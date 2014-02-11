using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    internal class Buf
    {
        public byte[] Buffer;
        public int Offset;
        public int Size;
    }

    internal class PublisherClientToken
    {
        private const string Tag = "PublisherClientToken";

        private readonly Timer _keepAlive;
        private readonly BufferPool _bufferPool = new BufferPool();
        private readonly PublisherToken _parent;
        private readonly SocketAsyncEventArgs _sender;
        private Buf _buf;
        private readonly string[] _topics;
        private readonly ManualResetEventSlim _r = new ManualResetEventSlim(true);
        private readonly BlockingCollection<Buf> _q = new BlockingCollection<Buf>(100);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        private readonly Stream _writeStream;
        internal Socket Socket;
        internal IPEndPoint EndPoint;
        internal int Count;
        private readonly Buf _keepAliveMessage = new Buf{Buffer = new byte[4], Size = 4};

        internal PublisherClientToken(Socket socket, PublisherToken publisherToken, string topics)
        {
            Socket = socket;
            _writeStream = new NetworkStream(socket);
            _writeStream = new BufferedStream(_writeStream);
            _sender = new SocketAsyncEventArgs();
            _sender.Completed += CompletedSend;
            _sender.UserToken = this;
            _parent = publisherToken;
            _cancellationToken = _cancellationTokenSource.Token;

            if (!string.IsNullOrEmpty(topics))
                _topics = topics.Split(',');

            _keepAlive = new Timer(KeepAlive, null, 3000, 3000);
            ThreadPool.QueueUserWorkItem(Sender);
        }

        private void KeepAlive(object state)
        {
            if (Socket.Connected)
            {
                SendInternal(_keepAliveMessage);
            }
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

            Close(this);
        }

        internal void Send(string topic, byte[] message)
        {
            if (topic != null && _topics != null && !IsForTopic(topic)) return;

            int len;
            byte[] wrapMessage = PacketProtocol.WrapMessage(_bufferPool, message, out len);

            try
            {
                // XXX
                //_q.TryAdd(new Buf { Buffer = wrapMessage, Size = len }, 2, _cancellationToken);
                _q.Add(new Buf { Buffer = wrapMessage, Size = len }, _cancellationToken);
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
        }

        private bool IsForTopic(string topic)
        {
            return _topics.Any(t => t == topic || t == "*");
        }

        private void SendInternal(Buf message)
        {
            //Log.Debug(Tag, "Sending message..");

            try
            {
                var bytes = new byte[message.Size];
                Buffer.BlockCopy(message.Buffer, message.Offset, bytes, 0, message.Size);
                _bufferPool.Free(message.Buffer);

                _writeStream.Write(bytes, 0, message.Size);
            }
            catch (SocketException)
            {
                Close(this);
            }
            catch (IOException)
            {
                Close(this);
            }
            return;

            _r.Wait();
            _r.Reset();
            _buf = message;
            _sender.SetBuffer(message.Buffer, message.Offset, message.Size);
            if (!Socket.SendAsync(_sender)) CompletedSend(null, _sender);
        }

        private void FreeBuffer()
        {
            _bufferPool.Free(_buf.Buffer);
            _buf = null;
        }

        private static void CompletedSend(object sender, SocketAsyncEventArgs e)
        {
            var pct = (PublisherClientToken)e.UserToken;

            //Log.Debug(Tag, "Completed send: {0}", e.SocketError);

            pct.FreeBuffer();

            if (e.SocketError == SocketError.Success)
            {
            }
            else
            {
                Close(pct);
            }
            pct._r.Set();
        }

        internal static void Close(PublisherClientToken pct)
        {
            Log.Debug(Tag, "Closing socket");

            var socket = pct.Socket;
            pct._parent.RemoveSubscriber(socket);
            pct._cancellationTokenSource.Cancel();

            try
            {
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException e)
            {
                Log.Debug(Tag, "Error in shutdown: SocketErrorCode: {0}", e.SocketErrorCode);
            }
            socket.Close();
        }
    }
}