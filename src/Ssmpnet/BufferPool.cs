using System.Threading;

namespace Ssmpnet
{
    internal static class BufferPool
    {
        const int PoolSize = 100;
        static readonly object[] Pool = new object[PoolSize];
        public const int BufferLength = 100 * 1024;

        public static byte[] Alloc(int size)
        {
            if (size > BufferLength) return new byte[size];

            for (int i = 0; i < Pool.Length; i++)
            {
                object tmp;
                if ((tmp = Interlocked.Exchange(ref Pool[i], null)) != null)
                    return (byte[])tmp;
            }
            return new byte[BufferLength];
        }

        public static void Free(byte[] buffer)
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
        }

        public static void Flush()
        {
            for (int i = 0; i < Pool.Length; i++)
            {
                Interlocked.Exchange(ref Pool[i], null); // and drop the old value on the floor
            }
        }
    }
}