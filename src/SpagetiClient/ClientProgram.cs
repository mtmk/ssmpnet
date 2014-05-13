using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using SpagetiLib;
using SpagetiLib.Net;
using Ssmpnet;

namespace SpagetiClient
{
    class ClientProgram
    {
        private const int Port = 58974;

        static void Main(string[] args)
        {
            typeof(ClientProgram).Run(args);
        }

        public static void SubPkg(string[] args)
        {
            SubscriberSocket.Start(new IPEndPoint(IPAddress.Loopback, Port), (buf, offset, size) =>
            {
                var memoryStream = new MemoryStream(buf, offset, size);
                var m = Serializer.Deserialize<SpagetiMessage1>(memoryStream);
                Console.WriteLine("[MESSAGE] Id:{0} Name:{1}", m.Id, m.Name);
            });
            Console.ReadLine();
        }

        public static void Sub(string[] args)
        {
            var sub = new SubscriberProtobufSocket<SpagetiMessageHeader>(new IPEndPoint(IPAddress.Loopback, Port));

            sub.Subscribe(
                h=> typeof(SpagetiMessage1),
                (h, m) =>
                {
                    var message = (SpagetiMessage1) m;
                    //if (h.Id % 100000 == 0)
                    {
                        Console.WriteLine("[MESSAGE] Id:{0} Type:{1} Time:{2:T} [Id:{3} Name:{4}]", h.Id,
                            h.SpagetiMessageType, h.Timestamp.ToDateTimeOfEpoch(), message.Id, message.Name);
                    }
                });

            Console.ReadLine();
            
            Console.WriteLine("Closing..");
            sub.Close();

            Console.WriteLine("Exiting..");
        }

        public static void Simple(string[] args)
        {
            Console.ReadLine();

            var client = new TcpClient();
            client.Connect(IPAddress.Loopback, Port);
            var networkStream = client.GetStream();

            while (true)
            {
                var header = Serializer.DeserializeWithLengthPrefix<SpagetiMessageHeader>(networkStream, PrefixStyle.Fixed32);
                var message = Serializer.DeserializeWithLengthPrefix<SpagetiMessage1>(networkStream, PrefixStyle.Fixed32);

                if (header.Id % 100000 == 0)
                {
                    Console.WriteLine(new string('_', 78));
                    Console.WriteLine("Id:{0} Type:{1} Time:{2}", header.Id, header.SpagetiMessageType, header.Timestamp.ToDateTimeOfEpoch());
                    Console.WriteLine("Id:{0} Name:{1}", message.Id, message.Name);
                }
            }
        }
    }
}
