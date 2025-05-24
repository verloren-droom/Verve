namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;


    public sealed partial class JsonSerializableService : SerializableServiceBase
    {
        public override T Deserialize<T>(byte[] value)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(value));
        }

        public override byte[] Serialize(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

        public override void Serialize(Stream stream, object obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(jsonString);
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string jsonString = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
        }
        
    }
}