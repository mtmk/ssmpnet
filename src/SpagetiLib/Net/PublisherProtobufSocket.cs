using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using ProtoBuf.Meta;

namespace SpagetiLib.Net
{
    public class PublisherProtobufSocket<THeader>
    {
        private const string Tag = "publisher";

        private readonly IPEndPoint _endPoint;
        private TcpListener _server;
        private readonly int _queueSize;
        private CancellationTokenSource _cancellation;
        private readonly ConcurrentDictionary<Client, Client> _clients = new ConcurrentDictionary<Client, Client>();
        private Thread _acceptTask;
        private List<Thread> _clientTasks;

        class Payload
        {
            public THeader Header;
            public object Message;
        }

        class Client
        {
            public TcpClient TcpClient;
            public NetworkStream NetworkStream;
            public BlockingCollection<Payload> Queue;
            public string Name;
        }

        public PublisherProtobufSocket(IPEndPoint endPoint, int queueSize = 1000)
        {
            _endPoint = endPoint;
            _queueSize = queueSize;
        }

        public void Publish(THeader header, object message)
        {
            foreach (var client in _clients.Keys)
            {
                var tryAdd = client.Queue.TryAdd(new Payload {Header = header, Message = message});
                if (tryAdd)
                    Log.Debug(Tag, "Sent message to client " + client.Name);
                else
                    Log.Debug(Tag, "Queue full for client " + client.Name);
            }
        }

        public void Start()
        {
            _server = new TcpListener(_endPoint);
            _server.Start();

            _cancellation = new CancellationTokenSource();
            _clientTasks = new List<Thread>();
            _acceptTask = new Thread(AcceptClient){IsBackground = true, Name = "Accept"};
            _acceptTask.Start();

            Log.Info(Tag, "Publisher started");
        }

        private void ConsumeQueue(object state)
        {
            var client = (Client)state;
            var token = _cancellation.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Payload payload;
                    var tryTake = client.Queue.TryTake(out payload, 100, token);
                    if (!tryTake || payload == null) continue;
                    Serializer.SerializeWithLengthPrefix(client.NetworkStream, payload.Header, PrefixStyle.Fixed32);
                    RuntimeTypeModel.Default.SerializeWithLengthPrefix(client.NetworkStream, payload.Message, payload.Message.GetType(), PrefixStyle.Fixed32, 0);
                }
                catch (Exception e)
                {
                    Log.Debug(Tag, "Client connection error: " + e.Message);
                    Log.Info(Tag, "Subscriber disconnected (" + client.Name + ")");
                    Client _; _clients.TryRemove(client, out _);
                    break;
                }
            }
            try { client.NetworkStream.Close(3000); } catch { }
            try { client.TcpClient.Close(); } catch { }
        }

        private void AcceptClient()
        {
            var token = _cancellation.Token;

            while (!token.IsCancellationRequested)
            {
                if (!_server.Pending())
                {
                    if(token.WaitHandle.WaitOne(100)) break;
                    continue;
                }

                var tcpClient = _server.AcceptTcpClient();

                var client = new Client
                              {
                                  TcpClient = tcpClient,
                                  NetworkStream = tcpClient.GetStream(),
                                  Queue = new BlockingCollection<Payload>(_queueSize)
                              };
                client.Name = client.GetHashCode().ToString();

                Log.Info(Tag, "Subscriber connected (" + client.Name + ")");

                _clients.TryAdd(client, client);

                var clientTask = new Thread(ConsumeQueue) {IsBackground = true, Name = "Client"};
                clientTask.Start(client);

                _clientTasks.Add(clientTask);
            }

            _server.Stop();
        }

        public void Stop()
        {
            Log.Info(Tag, "Publisher stopping");

            _cancellation.Cancel();

            _acceptTask.Join();

            foreach (var clientTask in _clientTasks)
            {
                clientTask.Join();
            }

            foreach (var client in _clients.Keys)
            {
                client.TcpClient.Close();
            }

            _server = null;
            _cancellation = null;
            _clientTasks = null;
            _acceptTask = null;
        }
    }
}