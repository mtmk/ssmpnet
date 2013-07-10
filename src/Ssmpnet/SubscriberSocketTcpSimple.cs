using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ssmpnet
{
    public class SubscriberSocketTcpSimple
    {
        private readonly Uri _uri;
        private readonly Action<byte[]> _callback;
        private Task _task;

        public SubscriberSocketTcpSimple(Uri uri, Action<byte[]> callback)
        {
            if (uri.Scheme.ToLowerInvariant() != "tcp")
                throw new ArgumentException("Only accepts 'tcp'", "uri");

            if (callback == null)
                throw new ArgumentNullException("callback");

            _uri = uri;
            _callback = callback;
        }

        public void Start(CancellationToken cancellationToken)
        {
            _task = Task.Factory.StartNew(() => GetValue(cancellationToken));
        }

        private void GetValue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NetworkStream networkStream = null;
                var pp = new PacketProtocol(8 * 1024 *1024) {MessageArrived = _callback};
                var buf = new byte[1024];
                try
                {
                    var tcpClient = new TcpClient();
                    tcpClient.Connect(_uri.Host, _uri.Port);
                    networkStream = tcpClient.GetStream();
                }
                catch (Exception e)
                {
                    Console.WriteLine("conn err: " + e.Message);
                }
                if (networkStream != null)
                {
                    try
                    {
                        while (true)
                        {
                            int read = networkStream.Read(buf, 0, buf.Length);
                            var pbuf = new byte[read];
                            Buffer.BlockCopy(buf, 0, pbuf, 0, read);
                            pp.DataReceived(pbuf);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("recv err: " + e.Message);
                    }
                }
            }
        }

        public void Wait()
        {
            if (_task != null)
                _task.Wait();
        }
    }
}