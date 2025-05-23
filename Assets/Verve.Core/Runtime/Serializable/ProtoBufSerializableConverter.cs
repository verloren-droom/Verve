namespace Verve.Serializable
{
    using ProtoBuf;
    using System.IO;
    using Google.Protobuf;


    public sealed partial class ProtoBufSerializableConverter : SerializableConverterBase
    {
        public override byte[] Serialize(object obj)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, obj);
                return stream.ToArray();
            }
        }
        
        public override void Serialize(Stream stream, object obj)
        {
            Serializer.Serialize(stream, obj);
        }
        
        public override T Deserialize<T>(byte[] value)
        {
            using (var stream = new MemoryStream(value))
            {
                return Serializer.Deserialize<T>(stream);
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);
        }
    }
}