using ProtoBuf;

namespace ProtobufSockets.Tests
{
    [ProtoContract]
    public class Message
    {
        [ProtoMember(1)]
        public string Payload { get; set; }
    }
}