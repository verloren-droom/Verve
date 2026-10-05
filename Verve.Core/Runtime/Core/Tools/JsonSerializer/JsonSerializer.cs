namespace Verve
{
    using System;
    using System.IO;
    using System.Text;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#else
    using System.Text.Json;
#endif
    
    /// <summary>
    ///   <para>默认 JSON 序列化。</para>
    /// </summary>
    internal sealed class JsonSerializer : IJsonSerializer
    {
#if !UNITY_5_3_OR_NEWER
        /// <summary>
        ///   <para>JSON 序列化选项。</para>
        /// </summary>
        private static readonly JsonSerializerOptions s_Options = new JsonSerializerOptions { IncludeFields = true };
#endif
        /// <inheritdoc />
        public void Serialize(Stream stream, object obj, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
#if UNITY_5_3_OR_NEWER
            var jsonString = JsonUtility.ToJson(obj);
#else
            var jsonString = System.Text.Json.JsonSerializer.Serialize(obj, s_Options);
#endif
            using var writer = new StreamWriter(stream, encoding ?? new UTF8Encoding(false), 1024, leaveOpen: true);
            writer.Write(jsonString);
        }

        /// <inheritdoc />
        public T Deserialize<T>(Stream stream, Encoding encoding = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            var jsonString = reader.ReadToEnd();
#if UNITY_5_3_OR_NEWER
            return JsonUtility.FromJson<T>(jsonString);
#else
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, s_Options);
#endif
        }
    }
}