#if UNITY_5_3_OR_NEWER

namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;


    /// <summary>
    ///   <para>JSON序列化</para>
    ///   <para>采用Newtonsoft.Json库</para>
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(SerializableGameFeature), Description = "JSON序列化 - 采用Newtonsoft.Json库")]
    public sealed partial class JsonSerializer : SerializerSubmodule
    {
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

#endif