using System;
using System.IO;
using NUnit.Framework;
using ProtoBuf;


namespace Ssmpnet.Test
{
    [ProtoContract]
    public class Foo
    {
        [ProtoMember(1)]
        public int Number;
        [ProtoMember(2)]
        public string Name;
    }

    [TestFixture]
    public class ProtoTest
    {
        [Test]
        public void SimpleSer()
        {
            var stream = new MemoryStream();
            Serializer.Serialize(stream, new Foo {Number = 1, Name = "a"});
            stream.Position = 0;
            var foo = Serializer.Deserialize<Foo>(stream);
            Assert.AreEqual(1, foo.Number);
            Assert.AreEqual("a", foo.Name);
        }
        
        [Test]
        public void Idl()
        {
            Console.WriteLine(Serializer.GetProto<Foo>());
        }
    }
}