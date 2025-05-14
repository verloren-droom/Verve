namespace Verve.Serializable
{
    
    public abstract class SerializableConverterBase : ISerializableConverter
    {
        public abstract T Deserialize<T>(string value);
        public abstract string Serialize(object obj);
    }
    
}