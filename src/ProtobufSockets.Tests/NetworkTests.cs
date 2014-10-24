using System;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace ProtobufSockets.Tests
{
    public class NetworkTests
    {
        [Fact]
        public void Publisher_starts_with_an_ephemeral_port()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Console.WriteLine(listener.Server.RemoteEndPoint);
        }
    }
}