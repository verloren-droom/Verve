namespace Verve
{
    using System;
    using System.Text;
    
    
    /// <summary>
    ///   <para>压缩扩展方法。</para>
    /// </summary>
    public static class CompressionExtension
    {
        /// <summary>
        ///   <para>压缩字符串。</para>
        /// </summary>
        /// <param name="text">要压缩的字符串。</param>
        /// <param name="self">目标对象。</param>
        /// <param name="encoding">编码。</param>
        public static byte[] Compress(this ICompression self, string text, Encoding encoding = null)
        {
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(text);
            return self.Compress(bytes);
        }

        /// <summary>
        ///   <para>解压字符串。</para>
        /// </summary>
        /// <param name="compressedData">压缩的数据。</param>
        /// <param name="self">目标对象。</param>
        /// <param name="encoding">编码。</param>
        public static string DecompressToString(this ICompression self, byte[] compressedData, Encoding encoding = null)
        {
            var decompressedBytes = self.Decompress(compressedData);
            return (encoding ?? Encoding.UTF8).GetString(decompressedBytes);
        }
        
        /// <summary>
        ///   <para>压缩字节切片。</para>
        /// </summary>
        /// <param name="data">要压缩的数据切片。</param>
        /// <param name="self">目标对象。</param>
        public static byte[] Compress(this ICompression self, ReadOnlySpan<byte> data)
            => self.Compress(data.ToArray());
        
        /// <summary>
        ///   <para>解压字节切片。</para>
        /// </summary>
        /// <param name="compressedData">压缩的数据切片。</param>
        /// <param name="self">目标对象。</param>
        public static byte[] Decompress(this ICompression self, ReadOnlySpan<byte> compressedData)
            => self.Decompress(compressedData.ToArray());
    }
}
