namespace Verve
{
    /// <summary>
    ///   <para>加解密工具。</para>
    /// </summary>
    [GameTool("加解密", typeof(AesCrypto))]
    public interface ICrypto : IGameTool
    {
        /// <summary>
        ///   <para>加密数据。</para>
        /// </summary>
        /// <param name="data">明文；调用只借用。</param>
        /// <param name="key">密钥；格式由实现约定，调用只借用且不保留。</param>
        /// <returns>新建的密文。</returns>
        byte[] Encrypt(byte[] data, byte[] key);

        /// <summary>
        ///   <para>解密数据。</para>
        /// </summary>
        /// <param name="encrypted">密文；调用只借用。</param>
        /// <param name="key">密钥；格式由实现约定，调用只借用且不保留。</param>
        /// <returns>新建的明文。</returns>
        byte[] Decrypt(byte[] encrypted, byte[] key);
    }
}