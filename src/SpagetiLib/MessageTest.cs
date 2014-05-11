using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;
using Xunit;

namespace SpagetiLib
{
    public class MessageTest
    {
        [Fact]
        public void Serialize_header_message()
        {
            var dateTime = DateTime.UtcNow.ToMillisecondPrecision();
            var memoryStream = new MemoryStream();

            Serializer.SerializeWithLengthPrefix(memoryStream, new SpagetiMessageHeader { Id = 1, SpagetiMessageType = SpagetiMessageTypeEnum.SpagetiMesssage1, Timestamp = dateTime.ToEpoch() },
                PrefixStyle.Fixed32);

            var buf = memoryStream.ToArray();

            Console.WriteLine(buf.Length);

            {
                var header = Serializer.DeserializeWithLengthPrefix<SpagetiMessageHeader>(new MemoryStream(buf), PrefixStyle.Fixed32);

                Assert.Equal(1, header.Id);
                Assert.Equal(SpagetiMessageTypeEnum.SpagetiMesssage1, header.SpagetiMessageType);
                Assert.Equal(dateTime, header.Timestamp.ToDateTimeOfEpoch());
            }
            {
                var header = (SpagetiMessageHeader)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(new MemoryStream(buf), null, typeof (SpagetiMessageHeader), PrefixStyle.Fixed32, 0);
                Assert.Equal(1, header.Id);
                Assert.Equal(SpagetiMessageTypeEnum.SpagetiMesssage1, header.SpagetiMessageType);
                Assert.Equal(dateTime, header.Timestamp.ToDateTimeOfEpoch());
            }

        }

        [Fact]
        public void Serialize_message1()
        {
            var memoryStream = new MemoryStream();

            Serializer.Serialize(memoryStream, new SpagetiMessage1 {Id = "1", Name = "a"});

            var buf = memoryStream.ToArray();

            Console.WriteLine(buf.Length);

            var message1 = Serializer.Deserialize<SpagetiMessage1>(new MemoryStream(buf));

            Assert.Equal("1", message1.Id);
            Assert.Equal("a", message1.Name);
        }
    }
}
