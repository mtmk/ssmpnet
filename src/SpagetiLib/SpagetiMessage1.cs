using ProtoBuf;

namespace SpagetiLib
{
    [ProtoContract]
    public class SpagetiMessage1
    {
        [ProtoMember(1)]
        public string Id { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }
    }
}