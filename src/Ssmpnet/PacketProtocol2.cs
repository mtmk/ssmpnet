using System;

namespace Ssmpnet
{
    public class PacketProtocol2
    {
        public static byte[] WrapMessage(byte[] message)
        {
            // Get the length prefix for the message
            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);

            // Concatenate the length prefix and the message
            var ret = new byte[lengthPrefix.Length + message.Length];
            lengthPrefix.CopyTo(ret, 0);
            message.CopyTo(ret, lengthPrefix.Length);

            return ret;
        }

        /// <summary>
        /// Initializes a new <see cref="PacketProtocol"/>, limiting message sizes to the given maximum size.
        /// </summary>
        /// <param name="maxMessageSize">The maximum message size supported by this protocol.
        ///  This may be less than or equal to zero to indicate no maximum message size.</param>
        public PacketProtocol2(int maxMessageSize)
        {
            // We allocate the buffer for receiving message lengths immediately
            _lengthBuffer = new byte[sizeof(int)];
            _maxMessageSize = maxMessageSize;
        }
        public PacketProtocol2()
        {
            // We allocate the buffer for receiving message lengths immediately
            _lengthBuffer = new byte[sizeof(int)];
            _maxMessageSize = 0;
        }

        /// <summary>
        /// The buffer for the length prefix; this is always 4 bytes long.
        /// </summary>
        private readonly byte[] _lengthBuffer;

        /// <summary>
        /// The buffer for the data; this is null if we are receiving the length prefix buffer.
        /// </summary>
        private byte[] _dataBuffer;

        /// <summary>
        /// The number of bytes already read into the buffer
        ///  (the length buffer if <see cref="_dataBuffer"/> is null, otherwise the data buffer).
        /// </summary>
        private int _bytesReceived;

        /// <summary>
        /// The maximum size of messages allowed.
        /// </summary>
        private readonly int _maxMessageSize;

        /// <summary>
        /// Indicates the completion of a message read from the stream.
        /// </summary>
        /// <remarks>
        /// <para>This may be called with an empty message, indicating that the other end had sent a keepalive message.
        ///  This will never be called with a null message.</para>
        /// <para>This event is invoked from within a call to <see><cref>DataReceived</cref></see>.
        ///  Handlers for this event should not call <see><cref>DataReceived</cref></see>.</para>
        /// </remarks>
        public Action<byte[]> MessageArrived { get; set; }

        /// <summary>
        /// Notifies the <see cref="PacketProtocol2"/> instance that incoming data has been received from the stream.
        ///  This method will invoke <see cref="MessageArrived"/> as necessary.
        /// </summary>
        /// <remarks>
        /// <para>This method may invoke <see cref="MessageArrived"/> zero or more times.</para>
        /// <para>Zero-length receives are ignored. Many streams use a 0-length read to indicate the end of a stream,
        ///  but <see cref="PacketProtocol"/> takes no action in this case.</para>
        /// </remarks>
        /// <param name="data">The data received from the stream. Cannot be null.</param>
        /// <exception cref="System.Net.ProtocolViolationException">If the data received is not a properly-formed message.</exception>
        public void DataReceived(byte[] data)
        {
            // Process the incoming data in chunks, as the ReadCompleted requests it

            // Logically, we are satisfying read requests with the received data, instead of processing the
            //  incoming buffer looking for messages.

            int i = 0;
            while (i != data.Length)
            {
                // Determine how many bytes we want to transfer to the buffer and transfer them
                int bytesAvailable = data.Length - i;
                if (_dataBuffer == null)
                {
                    // We're reading into the length prefix buffer
                    int bytesRequested = _lengthBuffer.Length - _bytesReceived;

                    // Copy the incoming bytes into the buffer
                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, _lengthBuffer, _bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    // Notify "read completion"
                    ReadCompleted(bytesTransferred);
                }
                else
                {
                    // We're reading into the data buffer
                    int bytesRequested = _dataBuffer.Length - _bytesReceived;

                    // Copy the incoming bytes into the buffer
                    int bytesTransferred = Math.Min(bytesRequested, bytesAvailable);
                    Array.Copy(data, i, _dataBuffer, _bytesReceived, bytesTransferred);
                    i += bytesTransferred;

                    // Notify "read completion"
                    ReadCompleted(bytesTransferred);
                }
            }
        }

        /// <summary>
        /// Called when a read completes. Parses the received data and calls <see cref="MessageArrived"/> if necessary.
        /// </summary>
        /// <param name="count">The number of bytes read.</param>
        /// <exception cref="System.Net.ProtocolViolationException">If the data received is not a properly-formed message.</exception>
        private void ReadCompleted(int count)
        {
            // Get the number of bytes read into the buffer
            _bytesReceived += count;

            if (_dataBuffer == null)
                FillLengthBuffer();
            else
                FillDataBuffer();
        }

        private void FillDataBuffer()
        {
            if (_bytesReceived != _dataBuffer.Length)
            {
                // We haven't gotten all the data buffer yet: just wait for more data to arrive
            }
            else
            {
                // We've gotten an entire packet
                if (MessageArrived != null)
                    MessageArrived(_dataBuffer);

                // Start reading the length buffer again
                _dataBuffer = null;
                _bytesReceived = 0;
            }
        }

        private void FillLengthBuffer()
        {
            if (_bytesReceived != sizeof(int))
            {
                // We haven't gotten all the length buffer yet: just wait for more data to arrive
            }
            else
            {
                // We've gotten the length buffer
                int length = BitConverter.ToInt32(_lengthBuffer, 0);

                // Sanity check for length < 0
                if (length < 0)
                    throw new System.Net.ProtocolViolationException("Message length is less than zero");

                // Another sanity check is needed here for very large packets, to prevent denial-of-service attacks
                if (_maxMessageSize > 0 && length > _maxMessageSize)
                {
                    throw new System.Net.ProtocolViolationException("Message length "
                                                                    +
                                                                    length.ToString(
                                                                        System.Globalization.CultureInfo.InvariantCulture)
                                                                    + " is larger than maximum message size "
                                                                    +
                                                                    _maxMessageSize.ToString(
                                                                        System.Globalization.CultureInfo.InvariantCulture));
                }

                // Zero-length packets are allowed as keepalives
                if (length == 0)
                {
                    _bytesReceived = 0;
                    if (MessageArrived != null)
                        MessageArrived(new byte[0]);
                }
                else
                {
                    // Create the data buffer and start reading into it
                    _dataBuffer = new byte[length];
                    _bytesReceived = 0;
                }
            }
        }

        public void DataReceived(byte[] buffer, int offset, int count)
        {
            var bytes = new byte[count];
            Buffer.BlockCopy(buffer, offset, bytes, 0, count);
            DataReceived(bytes);
        }
    }
}