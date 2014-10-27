using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtobufSockets
{
    public class Publisher : IDisposable
    {
        const string Tag = "Pub";

        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<Socket, Client> _cs = new ConcurrentDictionary<Socket, Client>();

        public Publisher() : this(new IPEndPoint(IPAddress.Loopback, 0))
        {
        }

        public Publisher(IPEndPoint ipEndPoint)
        {
            _listener = new TcpListener(ipEndPoint);
            _listener.Start();
            _listener.BeginAcceptTcpClient(ClientAccept, null);
        }

        public IPEndPoint EndPoint
        {
            get
            {
                return (IPEndPoint)_listener.Server.LocalEndPoint;
            }
        }

        public void Publish<T>(T message)
        {
            Publish(null, message);
        }

        public void Publish<T>(string topic, T message)
        {
            if (topic != null)
                topic = topic.TrimEnd('*', '.');

            foreach (var client in _cs.Values)
            {
                Log.Info(Tag, "publishing message..");

                if (!Topic.Match(client.Topic, topic)) continue;
                
                client.Send(topic, typeof(T), message);
            }
        }

        public void Dispose()
        {
            foreach (var client in _cs.Values)
            {
                client.Close();
            }

            _listener.Stop();
        }

        private void ClientAccept(IAsyncResult ar)
        {
            TcpClient tcpClient = _listener.EndAcceptTcpClient(ar);
            _listener.BeginAcceptTcpClient(ClientAccept, null);

            Socket socket = null;
            try
            {
                tcpClient.NoDelay = true;
                tcpClient.LingerState.Enabled = true;
                tcpClient.LingerState.LingerTime = 0;

                Log.Info(Tag, "client connected..");

                NetworkStream networkStream = tcpClient.GetStream();

                var client = new Client(tcpClient, networkStream, _cs);

                socket = tcpClient.Client;
                _cs.TryAdd(socket, client);

                var topic = Serializer.DeserializeWithLengthPrefix<string>(networkStream, PrefixStyle.Base128);
                Log.Info(Tag, "client topic is.. " + topic);

                Serializer.SerializeWithLengthPrefix(networkStream, "OK", PrefixStyle.Base128);

                client.SetServerAck(topic);
            }
            catch (Exception e)
            {
                Log.Info(Tag, "ERROR3: {0} : {1}", e.GetType(), e.Message);
                if (socket != null)
                {
                    Client _; _cs.TryRemove(socket, out _);
                }
                tcpClient.Close();
            }
        }
    }

    internal static class Topic
    {
        internal static bool Match(string topic, string test)
        {
            if (test == null || topic == null) return true;

            test = test.ToLowerInvariant();
            topic = topic.ToLowerInvariant();

            foreach (var t in topic.Split(','))
            {
                var t1 = t;
                if (t.EndsWith("*"))
                {
                    t1 = t.TrimEnd('*', '.');
                    if (test.StartsWith(t1)) return true;
                }

                if (test == t1) return true;
            }

            return false;
        }
    }

    [ProtoContract]
    public class Header
    {
        [ProtoMember(1)]
        public string Type { get; set; }
        [ProtoMember(2)]
        public string Topic { get; set; }
    }

    internal class Client
    {
        private const string Tag = "Cli";

        class ObjectWrap
        {
            public Type Type;
            public object Object;
            public string Topic;
        }

        private readonly ManualResetEvent _connected = new ManualResetEvent(false);
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _networkStream;
        private readonly ConcurrentDictionary<Socket, Client> _cs;
        private readonly BlockingCollection<ObjectWrap> _q = new BlockingCollection<ObjectWrap>(100);
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Thread _consumerThread;
        private string _topic;

        internal Client(TcpClient tcpClient, NetworkStream networkStream, ConcurrentDictionary<Socket, Client> cs)
        {
            _tcpClient = tcpClient;
            _networkStream = networkStream;
            _cs = cs;
            _consumerThread = new Thread(Consumer) { IsBackground = true };
            _consumerThread.Start();
        }

        internal string Topic
        {
            get { return _topic; }
        }

        internal void Send(string topic, Type type, object message)
        {
            try
            {
                _q.TryAdd(new ObjectWrap { Topic = topic, Type = type, Object = message }, 10, _cancellation.Token);
                Log.Info(Tag, "message queued to be sent..");
            }
            catch (OperationCanceledException) { }
            catch (InvalidOperationException) { }
        }

        internal void SetServerAck(string topic)
        {
            _topic = topic;
            _connected.Set();
        }

        internal void Close()
        {
            _cancellation.Cancel();
            _tcpClient.Close();
            _consumerThread.Join();
        }

        private void Consumer()
        {
            Log.Info(Tag, "starting client consumer..");
            CancellationToken token = _cancellation.Token;
            RuntimeTypeModel model = RuntimeTypeModel.Default;

            _connected.WaitOne();
            while (true)
            {
                try
                {
                    ObjectWrap take = _q.Take(token);
                    Log.Info(Tag, "dequeue message to send over wire..");
                    var header = new Header {Type = take.Type.Name, Topic = take.Topic};
                    Serializer.SerializeWithLengthPrefix(_networkStream, header, PrefixStyle.Base128);
                    model.SerializeWithLengthPrefix(_networkStream, take.Object, take.Type, PrefixStyle.Base128, 0);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Log.Info(Tag, "ERROR1: {0} : {1}", e.GetType(), e.Message);
                    break;
                }
            }

            Client _; _cs.TryRemove(_tcpClient.Client, out _);
            _tcpClient.Close();

            Log.Info(Tag, "exiting client consumer..");
        }
    }

}