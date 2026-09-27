namespace Verve
{
    /// <summary>
    ///   <para>解压缩接口。</para>
    /// </summary>
    [GameTool("解压缩", typeof(GZipCompression))]
    public interface ICompression : IGameTool
    {
        /// <summary>
        ///   <para>压缩数据。</para>
        /// </summary>
        /// <param name="data">要压缩的数据。</param>
        byte[] Compress(byte[] data);
        /// <summary>
        ///   <para>解压数据。</para>
        /// </summary>
        /// <param name="compressedData">要解压的数据。</param>
        byte[] Decompress(byte[] compressedData);
    }
}