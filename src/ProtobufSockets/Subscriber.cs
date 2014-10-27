using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ProtobufSockets
{
    public class Subscriber : IDisposable
    {
        private const string Tag = "Sub";

        private int _indexEndPoint = -1;
        private readonly IPEndPoint[] _endPoint;
        private TcpClient _tcpClient;
        private Thread _consumerThread;
        private Action<object> _action;
        private NetworkStream _networkStream;
        private string _topic;
        private readonly object _disposeSync = new object();
        private bool _disposed;
        private readonly object _connectSync = new object();
        private readonly object _typeSync = new object();
        private Timer _reconnectTimer;
        private Type _type;

        public Subscriber(IPEndPoint[] endPoint)
        {
            _endPoint = endPoint;
        }

        public void Subscribe<T>(string topic, Action<T> action)
        {
            _action = m => action((T) m);

            _topic = topic;
            lock (_typeSync)
                _type = typeof (T);

            Connect();
        }

        public void FailOver()
        {
            Reconnect();
        }

        public void Dispose()
        {
            lock (_disposeSync)
                _disposed = true;

            _tcpClient.Close();

            lock (_connectSync)
                CleanExitConsumerThread();
        }

        private void Connect()
        {
            if (!Monitor.TryEnter(_connectSync)) return;
            try
            {
                try
                {
                    _indexEndPoint++;
                    if (_indexEndPoint == _endPoint.Length)
                        _indexEndPoint = 0;

                    if (_tcpClient != null)
                        _tcpClient.Close();

                    _tcpClient = new TcpClient {NoDelay = true, LingerState = {Enabled = true, LingerTime = 0}};

                    _tcpClient.Connect(_endPoint[_indexEndPoint]);

                    _networkStream = _tcpClient.GetStream();

                    Serializer.SerializeWithLengthPrefix(_networkStream, _topic, PrefixStyle.Base128);
                    var ack = Serializer.DeserializeWithLengthPrefix<string>(_networkStream, PrefixStyle.Base128);

                    CleanExitConsumerThread();

                    _consumerThread = new Thread(Consume) {IsBackground = true};
                    _consumerThread.Start();

                    Log.Info(Tag, "publisher ack.. " + ack);
                    Log.Info(Tag, "subscribing started..");
                }
                catch (InvalidOperationException)
                {
                    Log.Info(Tag, "cannot connect, reconnecting..");
                    Reconnect();
                }
                catch (SocketException)
                {
                    Log.Info(Tag, "cannot connect, reconnecting..");
                    Reconnect();
                }
                catch (Exception e)
                {
                    Log.Info(Tag, "ERROR1: {0} : {1}", e.GetType(), e.Message);
                    Reconnect();
                }
            }
            finally
            {
                Monitor.Exit(_connectSync);
            }
        }

        private void CleanExitConsumerThread()
        {
            if (_consumerThread == null) return;
            
            try
            {
                if (!_consumerThread.Join(1000))
                {
                    _consumerThread.Abort();
                }
            }
            catch(Exception e)
            {
                Log.Info(Tag, "IGNORE_ERROR1: {0} : {1}", e.GetType(), e.Message);
            }
        }

        private void Reconnect()
        {
            if (!Monitor.TryEnter(_connectSync)) return;
            try
            {
                try
                {
                    if (_reconnectTimer != null)
                        _reconnectTimer.Dispose();
                }
                catch (Exception e)
                {
                    Log.Info(Tag, "IGNORE_ERROR2: {0} : {1}", e.GetType(), e.Message);
                }
                _reconnectTimer = new Timer(_ => Connect(), null, 1000, Timeout.Infinite);
            }
            finally
            {
                Monitor.Exit(_connectSync);
            }
        }

        private void Consume()
        {
            Type type;
            lock (_typeSync)
                type = _type;

            var typeName = type.Name;

            RuntimeTypeModel model = RuntimeTypeModel.Default;

            Log.Info(Tag, "consume started..");

            while (true)
            {
                try
                {
                    var header = Serializer.DeserializeWithLengthPrefix<Header>(_networkStream, PrefixStyle.Base128);

                    if (header == null)
                    {
                        Log.Info(Tag, "cannot serialise network stream..");
                        lock (_disposeSync) if (_disposed) break;
                        Reconnect();
                        break;
                    }
                    
                    if (header.Type != typeName)
                    {
                        Serializer.DeserializeWithLengthPrefix<string>(_networkStream, PrefixStyle.Base128);
                        continue;
                    }
                    var message = model.DeserializeWithLengthPrefix(_networkStream, null, type, PrefixStyle.Base128, 0);

                    if (message == null)
                    {
                        Log.Info(Tag, "cannot serialise network stream..");
                        lock (_disposeSync) if (_disposed) break;
                        Reconnect();
                        break;
                    }

                    Log.Info(Tag, "got message..");

                    _action(message);
                }
                catch (IOException)
                {
                    Log.Info(Tag, "cannot read from publisher..");
                    lock (_disposeSync) if (_disposed) break;
                    Reconnect();
                    break;
                }
                catch (Exception e)
                {
                    Log.Info(Tag, "ERROR2: {0} : {1}", e.GetType(), e.Message);

                    lock (_disposeSync) if (_disposed) break;
                    Reconnect();
                    break;
                }
            }

            Log.Info(Tag, "consume exit..");
        }
    }
}