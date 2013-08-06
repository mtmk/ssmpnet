using System.Net;
using System.Net.Sockets;

namespace Ssmpnet.Test
{
    public class UserToken
    {
        public Socket Socket;
        public IPEndPoint EndPoint;
        public int Count;

        internal PacketProtocol PacketProtocol;

        public UserToken(Socket socket)
        {
            Socket = socket;
        }

        public UserToken(Socket socket, IPEndPoint endPoint)
        {
            Socket = socket;
            EndPoint = endPoint;
        }
    }
}