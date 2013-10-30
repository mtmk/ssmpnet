using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    internal class SubscriberClientToken
    {
        internal readonly SubscriberToken SubscriberToken;
        internal PacketProtocol PacketProtocol;
        private readonly BufferPool _bufferPool = new BufferPool();
        private readonly BlockingCollection<Buf> _q = new BlockingCollection<Buf>(100);
        private readonly CancellationTokenSource _c = new CancellationTokenSource();
        
        internal CancellationToken CancellationToken;

        internal Socket Socket;

        internal Config Config;

        internal SubscriberClientToken(SubscriberToken subscriberToken)
        {
            SubscriberToken = subscriberToken;
            Socket = SubscriberToken.Socket;
            Config = subscriberToken.Config;
            PacketProtocol = new PacketProtocol { MessageArrived = SubscriberToken.Receiver };
            CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(subscriberToken.CancellationToken, _c.Token).Token;

            ThreadPool.QueueUserWorkItem(ConsumeQueue);
        }

        public void Close()
        {
            try
            {
                _c.Cancel();
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
            foreach (var buffer in _q.GetConsumingEnumerable())
            {
                PacketProtocol.DataReceived(buffer.Buffer, buffer.Offset, buffer.Size);
                _bufferPool.Free(buffer.Buffer);
            }
        }
    }
}