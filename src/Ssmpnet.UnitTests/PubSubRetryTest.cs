using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Ssmpnet.UnitTests
{
    [TestFixture]
    public class PubSubRetryTest
    {
        [Test]
        public void Retry_should_terminate_working_threads()
        {
            
            const int port = 12345;

            Thread.Sleep(1000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);

            SubscriberToken sub = SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, port), (m, o, c) => { }, config: new Config { ReconnectTimeout = 250 });

            Thread.Sleep(3000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            Thread.Sleep(1000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            Thread.Sleep(1000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            Thread.Sleep(1000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
            Thread.Sleep(1000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);

            sub.Close();

            Thread.Sleep(3000);
            Console.WriteLine(Process.GetCurrentProcess().Threads.Count);
        }
    }
}