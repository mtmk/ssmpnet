using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using SpagetiLib;
using SpagetiLib.Net;
using Ssmpnet;

namespace SpagetiServer
{
    class ServerProgram
    {
        private const int Port = 58974;

        static void Main(string[] args)
        {
            typeof(ServerProgram).Run(args);
        }

        public static void PubPkg(string[] args)
        {
            var pub = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, Port));
                        int i = 0;
            while (true)
            {
                i++;
                var message = new SpagetiMessage1
                {
                    Id = "id" + i,
                    Name = "name" + i
                };
                var memoryStream = new MemoryStream();
                Serializer.Serialize(memoryStream, message);
                pub.Publish(memoryStream.ToArray());
            }
        }

        public static void Pub(string[] args)
        {
            var pub = new PublisherProtobufSocket<SpagetiMessageHeader>(new IPEndPoint(IPAddress.Any, Port));
            pub.Start();
            var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            int i = 0;
            var pubTask = new Thread(
                () =>
                {
                    while (!token.IsCancellationRequested)
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

                        Thread.Sleep(10);
                    }

                    Console.WriteLine("Stopping pub..");
                    pub.Stop();
                    Console.WriteLine("Exiting pub..");
                });
            pubTask.Start();

            Console.ReadLine();
            
            Console.WriteLine("Cancelling..");
            cancellation.Cancel();

            Console.WriteLine("Joining..");
            pubTask.Join();

            Console.WriteLine("Exiting..");
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
