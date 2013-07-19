using System;
using System.Text;
using Xunit;

namespace Ssmpnet.Test
{
    public class PacketProtocol2Tests
    {
        [Fact]
        public void T()
        {
            int numMessages = 0;
            var packetizer = new PacketProtocol2();
            packetizer.MessageArrived += message =>
            {
                Console.WriteLine("GOT MSG: >>>" + Encoding.UTF8.GetString(message) + "<<<");
                ++numMessages;
            };

            byte[] wrapMessage = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample"));
            int len = wrapMessage.Length;
            int len1 = len / 2;
            int len2 = len - len1;

            Console.WriteLine("len1: " + len1);
            Console.WriteLine("len2: " + len2);

            var buf1 = new byte[len1];
            var buf2 = new byte[len2];

            Buffer.BlockCopy(wrapMessage, 0, buf1, 0, len1);
            Buffer.BlockCopy(wrapMessage, len1, buf2, 0, len2);

            Console.WriteLine("buf1: " + Encoding.ASCII.GetString(buf1, 4, buf1.Length - 4));
            Console.WriteLine("buf2: " + Encoding.ASCII.GetString(buf2));

            packetizer.DataReceived(buf1);
            packetizer.DataReceived(buf2);

            Console.WriteLine("Num messages: {0}", numMessages);
            
            Assert.Equal(1, numMessages);
        }

        /*
         * Case 1:
         *   data is empty
         *    - do nothing
         *  2
         *    data is smaller than head
         *     keep reading head
         
         
         
         */
    }
}