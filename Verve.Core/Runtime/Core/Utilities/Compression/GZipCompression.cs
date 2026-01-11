namespace Verve
{
    using System.IO;
    using System.IO.Compression;
    
    
    /// <summary>
    ///   <para>GZip压缩</para>
    /// </summary>
    internal sealed class GZipCompression : InstanceBase<GZipCompression>, ICompression
    {
        public byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzipStream = new GZipStream(output, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

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