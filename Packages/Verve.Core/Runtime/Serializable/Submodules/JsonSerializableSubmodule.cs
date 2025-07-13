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

        public override T Deserialize<T>(byte[] value)
        {
            // 移除UTF-8 BOM
            if (value.Length >= 3 && value[0] == 0xEF && value[1] == 0xBB && value[2] == 0xBF)
            {
                byte[] newValue = new byte[value.Length - 3];
                System.Buffer.BlockCopy(value, 3, newValue, 0, newValue.Length);
                value = newValue;
            }
    
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