namespace Verve
{
    using System.IO;
    using System.IO.Compression;
    
    /// <summary>
    ///   <para>GZip 压缩。</para>
    /// </summary>
    internal sealed class GZipCompression : ICompression
    {
        /// <inheritdoc />
        public byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzipStream = new GZipStream(output, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        /// <inheritdoc />
        public byte[] Decompress(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var output = new MemoryStream();
            using (var gzipStream = new GZipStream(input, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}