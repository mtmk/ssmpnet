using System;
using System.Text;
using NUnit.Framework;

namespace Ssmpnet.Test
{
    [TestFixture]
    public class PacketProtocolTests
    {
        [Test]
        public void Test()
        {
            MultipleMessages(new PacketProtocol());
            MessageSplit(new PacketProtocol());

        }

        internal void MessageSplit(PacketProtocol packet)
        {
            int numMessages = 0;
            packet.MessageArrived += (message) =>
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

            packet.DataReceived(buf1);
            packet.DataReceived(buf2);

            Console.WriteLine("Num messages: {0}", numMessages);

            Assert.AreEqual(1, numMessages);
        }

        internal void MultipleMessages(PacketProtocol packet)
        {
            int numMessages = 0;
            packet.MessageArrived += (message) =>
                                         {
                                             Console.WriteLine("GOT MSG: >>>" + Encoding.UTF8.GetString(message) + "<<<");
                                             ++numMessages;
                                         };

            byte[] w1 = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample1"));
            byte[] w2 = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample2"));
            byte[] w3 = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample3"));
            var buf = new byte[w1.Length + w2.Length + w3.Length];

            Buffer.BlockCopy(w1, 0, buf, 0, w1.Length);
            Buffer.BlockCopy(w2, 0, buf, w1.Length, w2.Length);
            Buffer.BlockCopy(w3, 0, buf, w1.Length + w2.Length, w3.Length);

            packet.DataReceived(buf);

            Console.WriteLine("Num messages: {0}", numMessages);

            Assert.AreEqual(3, numMessages);
        }

        /*
         *  Case 1:
         *    data is empty
         *      - do nothing
         *  2
         *    data is smaller than head
         *      - keep reading head
         *  3
         *    data is same size or larger than head
         *      - 
         *  
         
         
         
         */
    }
}