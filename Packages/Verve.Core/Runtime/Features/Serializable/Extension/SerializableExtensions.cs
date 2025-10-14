namespace Verve.Serializable
{
    using System.IO;
    using System.Text;
    
    
    /// <summary>
    /// 序列化/反序列化扩展方法，用于简化操作
    /// </summary>
    public static class SerializableExtensions
    {
        /// <summary>
        /// 字节数组序列化
        /// </summary>
        public static byte[] SerializeToBytes(this ISerializable self, object obj)
        {
            using var ms = new MemoryStream();
            self.Serialize(ms, obj);
            return ms.ToArray();
        }
        
        /// <summary>
        /// 字节数组反序列化
        /// </summary>
        public static T DeserializeFromBytes<T>(this ISerializable self, byte[] value)
        {
            using var ms = new MemoryStream(value);
            return self.Deserialize<T>(ms);
        }
        
        /// <summary>
        /// 字符串序列化
        /// </summary>
        public static string SerializeToString(this ISerializable self, object obj, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(self.SerializeToBytes(obj));
        }
        
        /// <summary>
        /// 字符串反序列化
        /// </summary>
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
