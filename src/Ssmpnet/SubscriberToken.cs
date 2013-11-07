using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet
{
    public class SubscriberToken
    {
        private readonly CancellationTokenSource _c = new CancellationTokenSource();

        internal Timer Retry;

        internal string Topics;

        internal Socket Socket;
        
        internal IPEndPoint EndPoint;

        internal Action<byte[], int, int> Receiver { get; set; }

        internal Config Config;

        internal Action Connected;

        internal CancellationToken CancellationToken;

        internal SubscriberToken(IPEndPoint endPoint, Action<byte[], int, int> receiver)
        {
            EndPoint = endPoint;
            Receiver = receiver;
            CancellationToken = _c.Token;
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
    }
}