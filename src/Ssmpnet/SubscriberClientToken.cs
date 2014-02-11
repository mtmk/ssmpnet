using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    internal class SubscriberClientToken
    {
        private const string Tag = "SubscriberClientToken";
        internal readonly SubscriberToken SubscriberToken;
        internal PacketProtocol PacketProtocol;
        private readonly BufferPool _bufferPool = new BufferPool();
        private readonly BlockingCollection<Buf> _q = new BlockingCollection<Buf>(100);
        private readonly CancellationTokenSource _c = new CancellationTokenSource();
        private Timer _keepAlive;

        internal CancellationToken CancellationToken;

        internal Socket Socket;

        internal SubscriberClientToken(SubscriberToken subscriberToken)
        {
            SubscriberToken = subscriberToken;
            Socket = SubscriberToken.Socket;
            PacketProtocol = new PacketProtocol { MessageArrived = SubscriberToken.Receiver, KeepAlive = KeepAlive };
            CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(subscriberToken.CancellationToken, _c.Token).Token;

            _keepAlive = new Timer(KeepAliveTimer, null, 6000, 6000);
            ThreadPool.QueueUserWorkItem(ConsumeQueue);
        }

        readonly object _keepAliveTimerSync = new object();
        private void KeepAliveTimer(object state)
        {
            var tryEnter = Monitor.TryEnter(_keepAliveTimerSync);
            if (!tryEnter) return;

            try
            {
                DateTime tmp;
                lock (_keepAliveSync)
                    tmp = _lastKeepAlive;

                if ((DateTime.UtcNow - tmp) > TimeSpan.FromSeconds(10))
                {
                    Log.Info(Tag, "Did not receive any keep alives. Reconnecting..");
                    SubscriberSocket.Retry(this);
                }
            }
            finally
            {
                Monitor.Exit(_keepAliveTimerSync);
            }
        }

        private readonly object _keepAliveSync = new object();
        private DateTime _lastKeepAlive = DateTime.UtcNow;
        private void KeepAlive()
        {
            lock (_keepAliveSync)
                _lastKeepAlive = DateTime.UtcNow;
        }

        public void Close()
        {
            try
            {
                _c.Cancel();
                if (_keepAlive != null)
                    _keepAlive.Dispose();
            }
            catch (ObjectDisposedException) { }
            SubscriberSocket.Close(Socket);
        }

        internal void Enqueue(byte[] buffer, int offset, int count)
        {
            var bytes = _bufferPool.Alloc(count);

            Buffer.BlockCopy(buffer, offset, bytes, 0, count);

            try
            {
                // XXX
                // Block if the queue is full. We cannot discard buffers here since
                // there may be incomplete messages contained in them.
                _q.Add(new Buf { Buffer = bytes, Size = count }, CancellationToken);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { }
        }

        private void ConsumeQueue(object state)
        {
            Log.Info(Tag, "ConsumeQueue thread: Enter (thread {0})", Thread.CurrentThread.ManagedThreadId);

            try
            {
                foreach (var buffer in _q.GetConsumingEnumerable(CancellationToken))
                {
                    PacketProtocol.DataReceived(buffer.Buffer, buffer.Offset, buffer.Size);
                    _bufferPool.Free(buffer.Buffer);
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { }

            Log.Info(Tag, "ConsumeQueue thread: Exit (thread {0})", Thread.CurrentThread.ManagedThreadId);
        }
    }
}