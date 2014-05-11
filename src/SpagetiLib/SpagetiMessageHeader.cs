using ProtoBuf;

namespace SpagetiLib
{
    [ProtoContract]
    public class SpagetiMessageHeader
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public long Timestamp { get; set; }

        [ProtoMember(3)]
        public SpagetiMessageTypeEnum SpagetiMessageType { get; set; }
    }
}