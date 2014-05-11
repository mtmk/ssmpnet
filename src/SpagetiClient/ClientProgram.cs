using System;
using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using SpagetiLib;
using SpagetiLib.Net;

namespace SpagetiClient
{
    class ClientProgram
    {
        private const int Port = 58974;

        static void Main(string[] args)
        {
            typeof(ClientProgram).Run(args);
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
                        Console.WriteLine(new string('_', 78));
                        Console.WriteLine("Id:{0} Type:{1} Time:{2}", h.Id, h.SpagetiMessageType, h.Timestamp.ToDateTimeOfEpoch());
                        Console.WriteLine("Id:{0} Name:{1}", message.Id, message.Name);
                    }
                });

            Console.ReadLine();
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
