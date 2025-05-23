using System.IO;

namespace Verve.Serializable
{
    public abstract class SerializableConverterBase : ISerializableConverter
    {
        public abstract T Deserialize<T>(byte[] value);
        public abstract byte[] Serialize(object obj);
        public virtual void Serialize(Stream stream, object obj)
        {
            throw new System.NotImplementedException();
        }

        public virtual T DeserializeFromStream<T>(Stream stream)
        {
            throw new System.NotImplementedException();
        }
    }
}