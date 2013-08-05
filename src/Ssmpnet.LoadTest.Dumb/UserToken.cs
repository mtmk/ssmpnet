using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Ssmpnet.LoadTest.Dumb
{
    public class UserToken
    {
        public Socket Socket;
        public IPEndPoint EndPoint;
        public int Count;

        public UserToken(Socket socket)
        {
            Socket = socket;
            StopWatch = new Stopwatch();
        }

        public UserToken(Socket socket, IPEndPoint endPoint)
        {
            Socket = socket;
            EndPoint = endPoint;
            StopWatch = new Stopwatch();
        }

        public Action<byte[], int, int> DataReceived { get; set; }
        public UserToken ClientSocketUserToken { get; set; }
        public Stopwatch StopWatch { get; set; }
    }
}