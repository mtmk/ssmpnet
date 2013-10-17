using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ssmpnet.Test.ZeroByte
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ae = new SocketAsyncEventArgs
                         {
                             UserToken = new ServerToken{Socket = server}
                         };
            ae.Completed += ServerAccept;
            server.Bind(new IPEndPoint(IPAddress.Any, 63122));
            server.Listen(100);
            if (!server.AcceptAsync(ae)) ServerAccept(null, ae);

            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var e = new SocketAsyncEventArgs
                        {
                            UserToken = new ClientToken{Socket = client},
                            RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 63122)
                        };
            e.Completed += ClientConnect;
            if (!client.ConnectAsync(e)) ClientConnect(null, e);

            client.NoDelay = true;
            Thread.Sleep(1000);
            Send(client, "xxx");
            Thread.Sleep(1000);
            Send(client, "");
            client.Send(new byte[]{0});
            client.Send(Encoding.ASCII.GetBytes("dd"));
            Console.ReadLine();
        }

        static void Send(Socket socket, string msg)
        {
            var e = new SocketAsyncEventArgs();
            var buffer = Encoding.ASCII.GetBytes(msg);
            e.SetBuffer(buffer, 0, buffer.Length);
            socket.SendAsync(e);
        }

        static void ServerAccept(object sender, SocketAsyncEventArgs e)
        {
            var st = (ServerToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                Socket socket = e.AcceptSocket;
                e.AcceptSocket = null;
                if (!st.Socket.AcceptAsync(e)) ServerAccept(null, e);

                Log.Info("ServerAccept", "Client connected.. [RemoteEndPoint:{0}]", socket.RemoteEndPoint);

                var e2 = new SocketAsyncEventArgs {UserToken = new ServerToken {Socket = socket}};
                e2.Completed += ServerReceive;
                e2.SetBuffer(new byte[1024], 0, 1024);
                if (!socket.ReceiveAsync(e2)) ServerReceive(null, e2);
            }
            else
            {
                Log.Error("ServerAccept", "Error: CompletedAccept: {0}", e.SocketError);
            }
        }

        private static void ServerReceive(object sender, SocketAsyncEventArgs e)
        {
            var st = (ServerToken) e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred == 0)
                {
                    Log.Info("ServerReceive", "Zero bytes received");
                }
                else
                {
                    Log.Info("ServerReceive", "Message:" + Encoding.ASCII.GetString(e.Buffer, 0, e.BytesTransferred));
                }
                e.SetBuffer(0, 1024);
                if (!st.Socket.ReceiveAsync(e)) ServerReceive(null, e);
            }
            else
            {
                Log.Error("ServerReceive", "Error: CompletedAccept: {0}", e.SocketError);
            }

        }

        static void ClientConnect(object sender, SocketAsyncEventArgs e)
        {
            var st = (ClientToken)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                var buffer = new byte[1024];
                e.SetBuffer(buffer, 0, 1024);
                e.Completed -= ClientConnect;
                e.Completed += ClientReceive;
                if (!st.Socket.ReceiveAsync(e)) ClientReceive(null, e);
            }
            else
            {
                Log.Error("ClientConnect", "Error: CompletedConnect: {0}", e.SocketError);
            }
        }

        private static void ClientReceive(object sender, SocketAsyncEventArgs e)
        {
            var st = (ClientToken)e.UserToken;

            if (e.SocketError == SocketError.Success)
            {
                Log.Info("ClientReceive:Received", Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred));
                e.SetBuffer(0, e.Buffer.Length);
                if (!st.Socket.ReceiveAsync(e)) ClientReceive(null, e);
            }
            else
            {
                Log.Debug("ClientReceive", "Error: CompletedReceive: {0}", e.SocketError);
            }
        }
    }

    class ClientToken
    {
        internal Socket Socket;
    }

    class ServerToken
    {
        internal Socket Socket;
    }

    internal static class Log
    {
        internal static void Error(string tag, string format, params object[] args)
        {
            ConsoleWriteLine("ERROR", tag, format, args);
        }

        [Conditional("TRACE")]
        internal static void Info(string tag, string format, params object[] args)
        {
            ConsoleWriteLine("INFO", tag, format, args);
        }

        [Conditional("DEBUG")]
        internal static void Debug(string tag, string format, params object[] args)
        {
            ConsoleWriteLine("DEBUG", tag, format, args);
        }

        internal static void ConsoleWriteLine(string level, string tag, string format, params object[] args)
        {
            Console.WriteLine("[{0}] Thread[ID:{3} TP:{4}] {1} - {2}",
                level,
                tag,
                string.Format(format, args)
                , Thread.CurrentThread.ManagedThreadId
                , Thread.CurrentThread.IsThreadPoolThread
                );
        }
    }
}
