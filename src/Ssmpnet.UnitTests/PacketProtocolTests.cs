using System;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Ssmpnet.UnitTests
{
    [TestFixture]
    public class PacketProtocolTests
    {
        [Test]
        //         Message
        //  [===================]
        //  |       |           |
        //     buf1      buf2   
        public void MessageSplit()
        {
            var packet = new PacketProtocol();
            int numMessages = 0;
            packet.MessageArrived += (message, o, c) =>
            {
                Console.WriteLine("GOT MSG: >>>" + Encoding.UTF8.GetString(message, o, c) + "<<<");
                ++numMessages;
            };

            int len;
            byte[] wrapMessage = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample"), out len);
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

            packet.DataReceived(buf1, 0, len1);
            packet.DataReceived(buf2, 0, len2);

            Console.WriteLine("Num messages: {0}", numMessages);

            Assert.AreEqual(1, numMessages);
        }

        [Test]
        //     Message1       Message2
        //  [============][================]
        //  |       |           |          |
        //     buf1      buf2       buf3
        public void MessageSplitMulti()
        {
            var packet = new PacketProtocol();
            int numMessages = 0;
            packet.MessageArrived += (message, o, c) =>
            {
                Console.WriteLine("GOT MSG: >>>" + Encoding.UTF8.GetString(message, o, c) + "<<<");
                ++numMessages;
            };

            int lenMsg1;
            int lenMsg2;
            byte[] wrapMessage1 = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample1"), out lenMsg1);
            byte[] wrapMessage2 = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("HelloWorldExample2"), out lenMsg2);

            int totalLen = (lenMsg1 + lenMsg2);

            Console.WriteLine("lenMsg1: " + lenMsg1);
            Console.WriteLine("lenMsg2: " + lenMsg2);
            Console.WriteLine("totalLen: " + totalLen);

            byte[] msg = new byte[totalLen];
            for (int i = 0; i < totalLen; i++) msg[i] = 65;
            Buffer.BlockCopy(wrapMessage1, 0, msg, 0, lenMsg1);
            Buffer.BlockCopy(wrapMessage2, 0, msg, lenMsg1, lenMsg2);


            Console.WriteLine("wrapMessage1: " + P(wrapMessage1));
            Console.WriteLine("wrapMessage2: " + P(wrapMessage2));
            Console.WriteLine("msg: " + P(msg));

            int len1 = totalLen / 3;
            int len2 = (totalLen - len1) / 2;
            int len3 = totalLen - len1 - len2;


            Console.WriteLine("len1: " + len1);
            Console.WriteLine("len2: " + len2);
            Console.WriteLine("len3: " + len3);

            var buf1 = new byte[len1];
            var buf2 = new byte[len2];
            var buf3 = new byte[len3];

            Buffer.BlockCopy(msg, 0, buf1, 0, len1);
            Buffer.BlockCopy(msg, len1, buf2, 0, len2);
            Buffer.BlockCopy(msg, len1 + len2, buf3, 0, len3);

            Console.WriteLine("buf1: " + P(buf1));
            Console.WriteLine("buf2: " + P(buf2));
            Console.WriteLine("buf2: " + P(buf3));

            packet.DataReceived(buf1, 0, len1);
            packet.DataReceived(buf2, 0, len2);
            packet.DataReceived(buf3, 0, len3);

            Console.WriteLine("Num messages: {0}", numMessages);

            Assert.AreEqual(2, numMessages);
        }

        static string P(byte[] buf)
        {
            return P(buf, 0, buf.Length);
        }

        static string P(byte[] buf, int o, int s)
        {
            return Regex.Replace(Encoding.UTF8.GetString(buf, o, s), @"[^\x20-\x7F]", ".");
        }
    }
}
