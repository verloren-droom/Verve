namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    
    
    /// <summary>
    ///   <para>序列化/反序列化扩展方法</para>
    ///   <para>用于简化操作</para>
    /// </summary>
    public static class SerializableExtensions
    {
        /// <summary>
        ///   <para>字节数组序列化</para>
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>
        ///   <para>序列化后二进制数据</para>
        /// </returns>
        public static byte[] SerializeToBytes(this ISerializable self, object obj)
        {
            using var ms = new MemoryStream();
            self.Serialize(ms, obj);
            return ms.ToArray();
        }
        
        /// <summary>
        ///   <para>字节数组反序列化</para>
        /// </summary>
        /// <param name="value">二进制数据</param>
        /// <returns>
        ///   <para>反序列化对象</para>
        /// </returns>
        public static T DeserializeFromBytes<T>(this ISerializable self, byte[] value)
        {
            using var ms = new MemoryStream(value);
            return self.Deserialize<T>(ms);
        }
        
        /// <summary>
        ///   <para>字符串序列化</para>
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="encoding">编码</param>
        /// <returns>
        ///   <para>序列化后字符串</para>
        /// </returns>
        public static string SerializeToString(this ISerializable self, object obj, Encoding encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetString(self.SerializeToBytes(obj));
        }
        
        /// <summary>
        ///   <para>字符串反序列化</para>
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="encoding">编码</param>
        /// <returns>
        ///   <para>反序列化对象</para>
        /// </returns>
        public static T DeserializeFromString<T>(this ISerializable self, string value, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var bytes = encoding.GetBytes(value);

            // 处理UTF-8 BOM
            if (encoding == Encoding.UTF8 && bytes.Length >= 3 && 
                bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                bytes = bytes[3..];
            }

            return self.DeserializeFromBytes<T>(bytes);
        }
    }
}
