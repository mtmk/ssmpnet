using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using ProtoBuf.Meta;

namespace SpagetiLib.Net
{
    public class SubscriberProtobufSocket<THeader>
    {
        private const string Tag = "subscriber";

        private readonly IPEndPoint _endPoint;
        private readonly int _queueSize;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private BlockingCollection<Payload> _queue;
        private CancellationTokenSource _cancellation;
        private Thread _consumerTask;
        private Action<THeader, object> _receiver;
        private Thread _readerTask;
        private Func<THeader, Type> _messageTypeResolver;
        private Thread _connectTask;

        private readonly object _connectSync = new object();

        class Payload
        {
            public THeader Header;
            public object Message;
        }

        public SubscriberProtobufSocket(IPEndPoint endPoint, int queueSize = 1000)
        {
            _endPoint = endPoint;
            _queueSize = queueSize;
        }

        public void Subscribe<T>(Action<T> receiver)
        {
            Subscribe(h => typeof (T), (h, m) => receiver((T)m));
        }

        public void Subscribe(Func<THeader, Type> messageTypeResolver, Action<THeader, object> receiver)
        {
            _messageTypeResolver = messageTypeResolver;
            _receiver = receiver;
            _cancellation = new CancellationTokenSource();
            _queue = new BlockingCollection<Payload>(_queueSize);
            _connectTask = new Thread(Connect){IsBackground = true, Name = "Connect"};
            _connectTask.Start();
        }

        private void ReConnect()
        {
            ThreadPool.QueueUserWorkItem(_ =>
                                         {
                                             if (!Monitor.TryEnter(_connectSync)) return;
                                             try
                                             {
                                                 _cancellation.Cancel();

                                                 Log.Info(Tag, "Reconnecting..");
                                                 _readerTask.Join();
                                                 _consumerTask.Join();
                                                 _connectTask.Join();

                                                 _cancellation = new CancellationTokenSource();
                                                 var token = _cancellation.Token;

                                                 _queue = new BlockingCollection<Payload>(_queueSize);
                                                 _connectTask = new Thread(Connect)
                                                                {
                                                                    IsBackground = true,
                                                                    Name = "Connect"
                                                                };
                                                 _connectTask.Start();
                                             }
                                             finally
                                             {
                                                 Monitor.Exit(_connectSync);
                                             }
                                         });
        }

        private void Connect()
        {
            var token = _cancellation.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    _tcpClient = new TcpClient();//{ReceiveTimeout = 10, SendTimeout = 10};
                    _tcpClient.Connect(_endPoint);
                    _networkStream = _tcpClient.GetStream();
                    break;
                }
                catch (Exception e)
                {
                    Log.Debug(Tag, "Cannot connect: " + e.Message);
                    Log.Info(Tag, "Cannot connect (retry in a second)");
                    token.WaitHandle.WaitOne(1000);
                }
            }

            Log.Debug(Tag, "Subscription started");

            _readerTask = new Thread(ReadMessages){IsBackground = true, Name = "Reader"};
            _readerTask.Start();
            _consumerTask = new Thread(ConsumeQueue){IsBackground = true, Name = "Consumer"};
            _consumerTask.Start();

            Log.Info(Tag, "Connected");
            Log.Debug(Tag, "(connect task exiting..)");
        }

        private void ReadMessages()
        {
            var token = _cancellation.Token;

            while (!token.IsCancellationRequested)
            {
                Log.Debug(Tag, "Queuing message");

                try
                {
                    var header = Serializer.DeserializeWithLengthPrefix<THeader>(_networkStream, PrefixStyle.Fixed32);
                    var message = RuntimeTypeModel.Default.DeserializeWithLengthPrefix(_networkStream, null, _messageTypeResolver(header), PrefixStyle.Fixed32, 0);
                    _queue.Add(new Payload { Header = header, Message = message }, token);
                }
                catch (Exception e)
                {
                    Log.Debug(Tag, "Cannot receive message: " + e.Message);
                    Log.Info(Tag, "Publisher disconnected");
                    ReConnect();
                }
            }

            Log.Debug(Tag, "(reader task exiting..)");
        }

        private void ConsumeQueue()
        {
            var token = _cancellation.Token;

            while (!token.IsCancellationRequested)
            {
                Payload payload = null;

                try
                {
                    var tryTake = _queue.TryTake(out payload, 100, token);
                    if (!tryTake) continue;
                }
                catch (OperationCanceledException)
                {
                }
                catch (ObjectDisposedException)
                {
                }

                if (payload == null) continue;

                Log.Debug(Tag, "Received message");

                try
                {
                    _receiver(payload.Header, payload.Message);
                }
                catch (Exception e)
                {
                    Log.Debug(Tag, "Error in receiver: " + e.Message);
                }
            }

            Log.Info(Tag, "(consumer task exiting..)");
        }

        public void Close()
        {
            _cancellation.Cancel();
            _connectTask.Join();
            _consumerTask.Join();
            _readerTask.Join();
            _tcpClient.Close();

            _cancellation = null;
            _queue = null;
            _tcpClient = null;
            _networkStream = null;
            _consumerTask = null;
            _receiver = null;
        }
    }
}