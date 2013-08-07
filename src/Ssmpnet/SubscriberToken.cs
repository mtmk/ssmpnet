using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    internal class SubscriberToken
    {
        private readonly PacketProtocol _packetProtocol;
        private readonly BlockingCollection<byte[]> _q = new BlockingCollection<byte[]>(1000);
        
        internal Socket Socket;
        
        internal IPEndPoint EndPoint;

        internal Action<byte[]> Receiver { get; set; }
        
        internal Action Connected;

        internal SubscriberToken(Socket socket, IPEndPoint endPoint, Action<byte[]> receiver)
        {
            Socket = socket;
            EndPoint = endPoint;
            Receiver = receiver;
            _packetProtocol = new PacketProtocol { MessageArrived = receiver };
            ThreadPool.QueueUserWorkItem(ConsumeQueue);
        }

        internal void Enqueue(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
            _q.Add(bytes);
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