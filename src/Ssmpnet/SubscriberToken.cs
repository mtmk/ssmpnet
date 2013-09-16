using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    public class SubscriberToken
    {
        private readonly PacketProtocol _packetProtocol;
        private readonly BlockingCollection<byte[]> _q = new BlockingCollection<byte[]>(1000);
        private readonly CancellationTokenSource _c = new CancellationTokenSource();
        
        internal Socket Socket;
        
        internal IPEndPoint EndPoint;

        internal Action<byte[]> Receiver { get; set; }
        
        internal Action Connected;

        internal CancellationToken CancellationToken;

        internal SubscriberToken(Socket socket, IPEndPoint endPoint, Action<byte[]> receiver)
        {
            Socket = socket;
            EndPoint = endPoint;
            Receiver = receiver;
            _packetProtocol = new PacketProtocol { MessageArrived = receiver };
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
            // TODO: Use buffer pool
            var bytes = new byte[count];
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);

            try
            {
                _q.TryAdd(bytes, 2, CancellationToken);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (OperationCanceledException) { }
        }

        private void ConsumeQueue(object state)
        {
            foreach (var buffer in _q.GetConsumingEnumerable())
            {
                _packetProtocol.DataReceived(buffer);
            }
        }
    }
}