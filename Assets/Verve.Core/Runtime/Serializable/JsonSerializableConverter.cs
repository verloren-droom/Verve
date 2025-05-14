namespace Verve.Serializable
{
    using Newtonsoft.Json;
    
    public sealed partial class JsonSerializableConverter : ISerializableConverter
    {
        public T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}