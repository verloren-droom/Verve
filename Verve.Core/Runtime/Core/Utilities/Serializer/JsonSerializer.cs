namespace Verve
{
    using System.IO;
    using System.Text;
    
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#else
    using System.Text.Json;
#endif
    
    
    /// <summary>
    ///   <para>JSON序列化</para>
    /// </summary>
    internal sealed class JsonSerializer : InstanceBase<JsonSerializer>, ISerializer
    {
        public void Serialize(Stream stream, object obj)
        {
#if UNITY_5_3_OR_NEWER
            var jsonString = JsonUtility.ToJson(obj);
#else
            var jsonString = JsonSerializer.Serialize(obj);
#endif
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(jsonString);
        }

        public T Deserialize<T>(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var jsonString = reader.ReadToEnd();
#if UNITY_5_3_OR_NEWER
            return JsonUtility.FromJson<T>(jsonString);
#else
            return JsonSerializer.Deserialize<T>(jsonString);
#endif
        }
    }
}