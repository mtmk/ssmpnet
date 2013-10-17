using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    public class SubscriberToken
    {
        internal PacketProtocol PacketProtocol;
        private readonly BlockingCollection<Buf> _q = new BlockingCollection<Buf>(1000);
        private readonly CancellationTokenSource _c = new CancellationTokenSource();

        internal string Topics;

        internal Socket Socket;
        
        internal IPEndPoint EndPoint;

        internal Action<byte[], int, int> Receiver { get; set; }
        
        internal Action Connected;

        internal CancellationToken CancellationToken;

        internal SubscriberToken(Socket socket, IPEndPoint endPoint, Action<byte[], int, int> receiver)
        {
            Socket = socket;
            EndPoint = endPoint;
            Receiver = receiver;
            PacketProtocol = new PacketProtocol { MessageArrived = receiver };
            CancellationToken = _c.Token;
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
            var bytes = BufferPool.Alloc(count);

            Buffer.BlockCopy(buffer, offset, bytes, 0, count);

            try
            {
                _q.TryAdd(new Buf{Buffer = bytes, Size = count}, 2, CancellationToken);
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
                BufferPool.Free(buffer.Buffer);
            }
        }
    }
}