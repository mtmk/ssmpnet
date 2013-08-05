using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ssmpnet.Dumb
{
    internal class PublisherClientToken
    {
        const string Tag = "PublisherClientToken";
        private const int BufferSize = 4 * 1024;
        readonly ManualResetEventSlim _r = new ManualResetEventSlim(true);

        internal Socket Socket;
        internal IPEndPoint EndPoint;
        internal int Count;
        readonly PublisherToken _parent;
        readonly SocketAsyncEventArgsPool _stack = new SocketAsyncEventArgsPool(100);
        readonly SemaphoreSlim _max = new SemaphoreSlim(100, 100);

        internal PublisherClientToken(Socket socket, PublisherToken publisherToken)
        {
            Socket = socket;

            for (int i = 0; i < 10000; i++)
            {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(new byte[BufferSize], 0, BufferSize);
                args.Completed += CompletedSend;
                args.UserToken = this;
            
                _stack.Push(args);
            }

            _parent = publisherToken;
        }

        internal void Send(byte[] message)
        {
            Log.Debug(Tag, "Sending message..");

            _max.Wait();
            var args = _stack.Pop();

            Buffer.BlockCopy(message, 0, args.Buffer, 0, message.Length);
            args.SetBuffer(0, message.Length);
            if (!Socket.SendAsync(args)) CompletedSend(null, args);
        }
        
        static void CompletedSend(object sender, SocketAsyncEventArgs e)
        {
            var pct = (PublisherClientToken)e.UserToken;
            Log.Debug(Tag, "Completed send: {0}", e.SocketError);

            if (e.SocketError == SocketError.Success)
            {
            }
            else
            {
                Close(pct);
            }

            pct._stack.Push(e);
            pct._max.Release();
        }

        static void Close(PublisherClientToken pct)
        {
            Log.Debug(Tag, "Closing socket");

            var socket = pct.Socket;
            pct._parent.RemoveSubscriber(socket);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            try
            {
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Send);
            }
            catch (SocketException e)
            {
                Log.Debug(Tag, "Error in shutdown: {0}", e.Message);
            }
            socket.Close();
        }
    }

    class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;

        // Initializes the object pool to the specified size
        //
        // The "capacity" parameter is the maximum number of 
        // SocketAsyncEventArgs objects the pool can hold
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        // Add a SocketAsyncEventArg instance to the pool
        //
        //The "item" parameter is the SocketAsyncEventArgs instance 
        // to add to the pool
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        // Removes a SocketAsyncEventArgs instance from the pool
        // and returns the object removed from the pool
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        // The number of SocketAsyncEventArgs instances in the pool
        public int Count
        {
            get { return m_pool.Count; }
        }

    }
}