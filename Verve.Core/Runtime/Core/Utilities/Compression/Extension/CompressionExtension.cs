namespace Verve
{
    using System;
    using System.Text;
    
    
    /// <summary>
    ///   <para>压缩扩展方法</para>
    /// </summary>
    public static class CompressionExtension
    {
        /// <summary>
        ///   <para>压缩字符串</para>
        /// </summary>
        /// <param name="text">要压缩的字符串</param>
        public static byte[] CompressString(this ICompression self, string text, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<byte>();
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(text);
            return self.Compress(bytes);
        }

        /// <summary>
        ///   <para>解压字符串</para>
        /// </summary>
        /// <param name="compressedData">压缩的数据</param>
        public static string DecompressToString(this ICompression self, byte[] compressedData, Encoding encoding = null)
        {
            if (compressedData == null || compressedData.Length == 0)
                return string.Empty;
                
            var decompressedBytes = self.Decompress(compressedData);
            return (encoding ?? Encoding.UTF8).GetString(decompressedBytes);
        }
    }
}