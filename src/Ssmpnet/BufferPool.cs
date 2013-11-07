using System.Collections.Generic;
using System.Threading;

namespace Ssmpnet
{
    // From Proto-buf - Marc Gravell
    internal class BufferPool
    {
        readonly object[] _pool;
        private readonly int _bufferLength;

        public BufferPool(int poolSize = 100, int bufferLength = 100*1024)
        {
            _pool = new object[poolSize];
            _bufferLength = bufferLength;
        }

        public byte[] Alloc(int size)
        {
            if (size > _bufferLength) return new byte[size];

            for (int i = 0; i < _pool.Length; i++)
            {
                object tmp;
                if ((tmp = Interlocked.Exchange(ref _pool[i], null)) != null)
                    return (byte[])tmp;
            }
            return new byte[_bufferLength];
        }

        public void Free(byte[] buffer)
        {
            if (buffer == null) return;
            if (buffer.Length == _bufferLength)
            {
                for (int i = 0; i < _pool.Length; i++)
                {
                    if (Interlocked.CompareExchange(ref _pool[i], buffer, null) == null)
                    {
                        break; // found a null; swapped it in
                    }
                }
            }
        }

        public void Flush()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                Interlocked.Exchange(ref _pool[i], null); // and drop the old value on the floor
            }
        }
    }

    // XXX
    // http://stackoverflow.com/a/530228/248393 - Marc Gravell
    internal class SizeQueue<T>
    {
        class Sizer
        {
            public T Obj;
            public int Size;
        }
        private readonly Queue<Sizer> _queue = new Queue<Sizer>();
        private readonly int _maxSize;
        private int _total;
        private readonly int _maxStorageBytes;
        bool _closing;

        public SizeQueue(int maxSize, int maxStorageBytes)
        {
            _maxSize = maxSize;
            _maxStorageBytes = maxStorageBytes;
        }

        public void Enqueue(T item, int size)
        {
            lock (_queue)
            {
                while (_queue.Count >= _maxSize || _total >= _maxStorageBytes)
                {
                    Monitor.Wait(_queue);
                }
                _total += size;
                _queue.Enqueue(new Sizer{Obj = item, Size = size});
                if (_queue.Count == 1)
                {
                    // wake up any blocked dequeue
                    Monitor.PulseAll(_queue);
                }
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_queue);
                }
                
                Sizer sizer = _queue.Dequeue();
                T item = sizer.Obj;
                _total -= sizer.Size;

                if (_queue.Count == _maxSize - 1 || _total < _maxStorageBytes)
                {
                    // wake up any blocked enqueue
                    Monitor.PulseAll(_queue);
                }
                return item;
            }
        }

        public void Close()
        {
            lock (_queue)
            {
                _closing = true;
                Monitor.PulseAll(_queue);
            }
        }

        public bool TryDequeue(out T value)
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    if (_closing)
                    {
                        value = default(T);
                        return false;
                    }
                    Monitor.Wait(_queue);
                }
                Sizer sizer = _queue.Dequeue();
                value = sizer.Obj;
                _total -= sizer.Size;
                if (_queue.Count == _maxSize - 1 || _total < _maxStorageBytes)
                {
                    // wake up any blocked enqueue
                    Monitor.PulseAll(_queue);
                }
                return true;
            }
        }
    }
}