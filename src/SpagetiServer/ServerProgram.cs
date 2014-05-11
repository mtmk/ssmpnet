using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using SpagetiLib;
using SpagetiLib.Net;

namespace SpagetiServer
{
    class ServerProgram
    {
        private const int Port = 58974;

        static void Main(string[] args)
        {
            typeof(ServerProgram).Run(args);
        }

        public static void Pub(string[] args)
        {
            var pub = new PublisherProtobufSocket<SpagetiMessageHeader>(new IPEndPoint(IPAddress.Any, Port));
            pub.Start();

            int i = 0;
            while (true)
            {
                pub.Publish(new SpagetiMessageHeader
                {
                    Id = ++i,
                    SpagetiMessageType = SpagetiMessageTypeEnum.SpagetiMesssage1,
                    Timestamp = DateTime.UtcNow.ToEpoch()
                },
                new SpagetiMessage1
                {
                    Id = "id" + i,
                    Name = "name" + i
                });
                Thread.Sleep(1000);
            }
            
        }

        public static void Simple(string[] args)
        {
            var server = new TcpListener(IPAddress.Any, Port);
            server.Start();
            var client = server.AcceptTcpClient();

            var networkStream = client.GetStream();


            int i = 0;
            while (true)
            {
                Serializer.SerializeWithLengthPrefix(networkStream,
                    new SpagetiMessageHeader
                    {
                        Id = ++i,
                        SpagetiMessageType = SpagetiMessageTypeEnum.SpagetiMesssage1,
                        Timestamp = DateTime.UtcNow.ToEpoch()
                    },
                    PrefixStyle.Fixed32);

                Serializer.SerializeWithLengthPrefix(networkStream,
                    new SpagetiMessage1
                    {
                        Id = "id" + i,
                        Name = "name" + i
                    }, PrefixStyle.Fixed32);


//                Thread.Sleep(10);
            }
        }
    }




}
