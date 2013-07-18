using System.Net;
using System.Net.Sockets;

namespace Ssmpnet
{
    public class UserToken
    {
        public Socket Socket;
        public IPEndPoint EndPoint;
        public int Count;
        public PacketProtocol PacketProtocol;

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