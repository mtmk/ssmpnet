using System;

namespace Ssmpnet
{
    // Original source: http://blog.stephencleary.com/2009/04/sample-code-length-prefix-message.html
    internal class PacketProtocol
    {
        private readonly byte[] _lengthBuffer = new byte[sizeof(uint)];
        private byte[] _dataBuffer;
        private int _bytesReceived;
        private int _length;
        private readonly BufferPool _bufferPool = new BufferPool();

        internal static byte[] WrapMessage(byte[] msg, out int length)
        {
            return WrapMessage(new BufferPool(), msg, out length);
        }

        internal static byte[] WrapMessage(BufferPool bufferPool, byte[] msg, out int length)
        {
            var len = BitConverter.GetBytes(msg.Length);

            //var buffer = new byte[len.Length + msg.Length];
            length = len.Length + msg.Length;
            var buffer = bufferPool.Alloc(length);
            
            Buffer.BlockCopy(len, 0, buffer, 0, len.Length);
            Buffer.BlockCopy(msg, 0, buffer, len.Length, msg.Length);

            return buffer;
        }

        internal Action<byte[], int, int> MessageArrived { get; set; }

        internal Action KeepAlive { get; set; }

        internal void DataReceived(byte[] data, int offset, int size)
        {
            int i = offset;
            var datalen = offset + size;
            while (i < datalen)
            {
                int bytesAvailable = datalen - i;
                if (_dataBuffer == null)
                {
                    int bytesRequested = _lengthBuffer.Length - _bytesReceived;

                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Buffer.BlockCopy(data, i, _lengthBuffer, _bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    ReadCompleted(bytesTransferred);
                }
                else
                {
                    int bytesRequested = _length - _bytesReceived;

                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Buffer.BlockCopy(data, i, _dataBuffer, _bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    ReadCompleted(bytesTransferred);
                }
            }
        }

        private void ReadCompleted(int count)
        {
            _bytesReceived += count;

            if (_dataBuffer == null)
                FillLengthBuffer();
            else
                FillDataBuffer();
        }

        private void FillLengthBuffer()
        {
            // We haven't gotten all the length buffer yet: just wait for more data to arrive
            if (_bytesReceived != sizeof(uint))
                return;

            _length = BitConverter.ToInt32(_lengthBuffer, 0);

            if (_length == 0)
            {
                _bytesReceived = 0;
                if (KeepAlive != null)
                    KeepAlive();
            }
            else
            {
                // Create the data buffer and start reading into it
                //_dataBuffer = new byte[length];
                _dataBuffer = _bufferPool.Alloc(_length);

                _bytesReceived = 0;
            }
        }

        private void FillDataBuffer()
        {
            // We haven't gotten all the data buffer yet: just wait for more data to arrive
            if (_bytesReceived != _length)
                return;

            if (MessageArrived != null)
                MessageArrived(_dataBuffer, 0, _length);

            _bufferPool.Free(_dataBuffer);
            _dataBuffer = null;
            _bytesReceived = 0;
        }
    }
}