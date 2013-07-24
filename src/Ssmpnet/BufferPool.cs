using System;
using System.Diagnostics;
using System.Threading;

namespace Ssmpnet
{


    public static class BufferPool
    {
        const int PoolSize = 20;
        static readonly object[] Pool = new object[PoolSize];
        public const int BufferLength = 1024;

        public static void Flush()
        {
            for (int i = 0; i < Pool.Length; i++)
            {
                Interlocked.Exchange(ref Pool[i], null); // and drop the old value on the floor
            }
        }

        public static byte[] GetBuffer()
        {
            for (int i = 0; i < Pool.Length; i++)
            {
                object tmp;
                if ((tmp = Interlocked.Exchange(ref Pool[i], null)) != null) return (byte[])tmp;
            }
            return new byte[BufferLength];
        }

        public static void DemandSpace(ref byte[] buffer, int requiredSize)
        {
            if (buffer.Length < requiredSize)
            {
                ResizeAndFlushLeft(ref buffer, requiredSize, 0, 0);
            }
        }

        public static void ResizeAndFlushLeft(ref byte[] buffer, int toFitAtLeastBytes, int copyFromIndex, int copyBytes)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(toFitAtLeastBytes > buffer.Length);
            Debug.Assert(copyFromIndex >= 0);
            Debug.Assert(copyBytes >= 0);

            // try doubling, else match
            int newLength = buffer.Length * 2;
            if (newLength < toFitAtLeastBytes) newLength = toFitAtLeastBytes;

            var newBuffer = new byte[newLength];
            if (copyBytes > 0)
            {
                Buffer.BlockCopy(buffer, copyFromIndex, newBuffer, 0, copyBytes);
            }
            if (buffer.Length == BufferLength)
            {
                ReleaseBufferToPool(ref buffer);
            }
            buffer = newBuffer;
        }

        public static void ReleaseBufferToPool(ref byte[] buffer)
        {
            if (buffer == null) return;
            if (buffer.Length == BufferLength)
            {

                for (int i = 0; i < Pool.Length; i++)
                {
                    if (Interlocked.CompareExchange(ref Pool[i], buffer, null) == null)
                    {
                        break; // found a null; swapped it in
                    }
                }
            }
            // if no space, just drop it on the floor
            buffer = null;
        }

    }
}