namespace Verve
{
    /// <summary>
    ///   <para>压缩接口</para>
    /// </summary>
    public interface ICompression
    {
        /// <summary>
        ///   <para>压缩数据</para>
        /// </summary>
        /// <param name="data">要压缩的数据</param>
        byte[] Compress(byte[] data);
        /// <summary>
        ///   <para>解压数据</para>
        /// </summary>
        /// <param name="compressedData">要解压的数据</param>
        byte[] Decompress(byte[] compressedData);
    }
}