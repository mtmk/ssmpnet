using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ssmpnet
{
    public class PublisherSocketTcpSimple
    {
        private readonly Uri _uri;

        private readonly ConcurrentDictionary<TcpClient, BlockingCollection<byte[]>>
            _clients = new ConcurrentDictionary<TcpClient, BlockingCollection<byte[]>>();

        public PublisherSocketTcpSimple(Uri uri)
        {
            if (uri.Scheme.ToLowerInvariant() != "tcp")
                throw  new ArgumentException("Only accepts 'tcp'", "uri");
            _uri = uri;
        }

        public void Publish(byte[] message)
        {
            foreach (var cq in _clients.Values)
                cq.TryAdd(PacketProtocol.WrapMessage(message), 2);
        }

        public void Start(CancellationToken cancellationToken)
        {
            var tcpListener = new TcpListener(IPAddress.Any, _uri.Port);
            tcpListener.Start();

            Task.Factory.StartNew(() => AcceptClients(cancellationToken, tcpListener));
        }

        private void AcceptClients(CancellationToken cancellationToken, TcpListener tcpListener)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = tcpListener.AcceptTcpClient();
                var cq = new BlockingCollection<byte[]>(100);
                _clients.TryAdd(client, cq);
                Task.Factory.StartNew(() => HandleClient(cancellationToken, client, cq));
            }
        }

        private void HandleClient(CancellationToken cancellationToken, TcpClient client, BlockingCollection<byte[]> cq)
        {
            NetworkStream networkStream = null;
            try
            {
                networkStream = client.GetStream();
            }
            catch (Exception e)
            {
                Console.WriteLine("conn err: " + e.Message);
            }

            if (networkStream != null)
            {
                try
                {
                    foreach (var m in cq.GetConsumingEnumerable(cancellationToken))
                    {
                        try
                        {
                            networkStream.Write(m, 0, m.Length);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("conn err:" + e.Message);
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }

            BlockingCollection<byte[]> x;
            _clients.TryRemove(client, out x);
        }
    }
}
