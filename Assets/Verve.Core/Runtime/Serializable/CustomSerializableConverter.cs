namespace Verve.Serializable
{
    public sealed partial class CustomSerializableConverter : ISerializableConverter
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