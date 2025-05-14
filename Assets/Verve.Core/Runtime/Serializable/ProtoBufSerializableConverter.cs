namespace Verve.Serializable
{
    // using ProtoBuf;
    
    public sealed class ProtoBufSerializableConverter : ISerializableConverter
    {
        public T Deserialize<T>(string value)
        {
            throw new System.NotImplementedException();
        }

        public string Serialize(object obj)
        {
            throw new System.NotImplementedException();
        }
    }
}