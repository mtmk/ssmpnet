using System;

namespace Ssmpnet.Test
{
    // Original source: http://blog.stephencleary.com/2009/04/sample-code-length-prefix-message.html
    internal class PacketProtocol
    {
        private readonly byte[] _lengthBuffer = new byte[sizeof(uint)];
        private byte[] _dataBuffer;
        private int _bytesReceived;

        internal static byte[] WrapMessage(byte[] msg)
        {
            byte[] len = BitConverter.GetBytes(msg.Length);
            
            var buffer = new byte[len.Length + msg.Length];

            Buffer.BlockCopy(len, 0, buffer, 0, len.Length);
            Buffer.BlockCopy(msg, 0, buffer, len.Length, msg.Length);

            return buffer;
        }

        internal Action<byte[]> MessageArrived { get; set; }

        internal Action KeepAlive { get; set; }

        internal void DataReceived(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
            DataReceived(bytes);
        }

        internal void DataReceived(byte[] data)
        {
            int i = 0;
            while (i < data.Length)
            {
                int bytesAvailable = data.Length - i;
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
                    int bytesRequested = _dataBuffer.Length - _bytesReceived;

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

            uint length = BitConverter.ToUInt32(_lengthBuffer, 0);

            if (length == 0)
            {
                _bytesReceived = 0;
                if (KeepAlive != null)
                    KeepAlive();
            }
            else
            {
                // Create the data buffer and start reading into it
                _dataBuffer = new byte[length];
                _bytesReceived = 0;
            }
        }

        private void FillDataBuffer()
        {
            // We haven't gotten all the data buffer yet: just wait for more data to arrive
            if (_bytesReceived != _dataBuffer.Length)
                return;

            if (MessageArrived != null)
                MessageArrived(_dataBuffer);

            _dataBuffer = null;
            _bytesReceived = 0;
        }
    }
}