namespace Verve
{
    using System;
    using System.IO;
    using System.Text;
    
    /// <summary>
    ///   <para>序列化/反序列化扩展方法。</para>
    /// </summary>
    public static class SerializerExtensions
    {
        /// <summary>
        ///   <para>字节数组序列化。</para>
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <returns>
        ///   <para>序列化后二进制数据</para>
        /// </returns>
        /// <param name="self">目标对象。</param>
        /// <param name="encoding">编码。</param>
        public static byte[] SerializeToBytes(this ISerializer self, object obj, Encoding encoding = null)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            using var ms = new MemoryStream();
            self.Serialize(ms, obj, encoding);
            return ms.ToArray();
        }
        
        /// <summary>
        ///   <para>字节数组反序列化。</para>
        /// </summary>
        /// <param name="value">二进制数据。</param>
        /// <returns>
        ///   <para>反序列化对象</para>
        /// </returns>
        /// <param name="self">目标对象。</param>
        /// <param name="encoding">编码。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static T Deserialize<T>(this ISerializer self, byte[] value, Encoding encoding = null)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (value == null) throw new ArgumentNullException(nameof(value));
            using var ms = new MemoryStream(value);
            return self.Deserialize<T>(ms, encoding);
        }
        
        /// <summary>
        ///   <para>字符串序列化。</para>
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <param name="encoding">编码。</param>
        /// <returns>
        ///   <para>序列化后字符串</para>
        /// </returns>
        /// <param name="self">目标对象。</param>
        public static string SerializeToString(this ISerializer self, object obj, Encoding encoding = null)
        {
            encoding ??= new UTF8Encoding(false);
            var bytes = self.SerializeToBytes(obj, encoding);
            var preamble = encoding.GetPreamble();
            int offset = bytes.AsSpan().StartsWith(preamble) ? preamble.Length : 0;
            return encoding.GetString(bytes, offset, bytes.Length - offset);
        }
        
        /// <summary>
        ///   <para>字符串反序列化。</para>
        /// </summary>
        /// <param name="value">字符串。</param>
        /// <param name="encoding">编码。</param>
        /// <returns>
        ///   <para>反序列化对象</para>
        /// </returns>
        /// <param name="self">目标对象。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        public static T Deserialize<T>(this ISerializer self, string value, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return self.Deserialize<T>(encoding.GetBytes(value), encoding);
        }
    }
}
