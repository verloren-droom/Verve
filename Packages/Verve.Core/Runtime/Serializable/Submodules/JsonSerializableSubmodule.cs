namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;


    /// <summary>
    /// JSON序列化子模块 - 采用Newtonsoft.Json库
    /// </summary>
    public sealed partial class JsonSerializableSubmodule : SerializableSubmodule
    {
        public override string ModuleName => "JsonSerializable";

        public override void Serialize(Stream stream, object obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(jsonString);
        }

        public override T Deserialize<T>(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string jsonString = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}