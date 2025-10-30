namespace Verve.Crypto
{
    /// <summary>
    ///   <para>加解密接口</para>
    /// </summary>
    public interface ICrypto
    {
        /// <summary>
        ///   <para>加密</para>
        /// </summary>
        /// <param name="data">待加密数据</param>
        /// <returns>
        ///   <para>加密后的数据</para>
        /// </returns>
        byte[] Encrypt(byte[] data);
        
        /// <summary>
        ///   <para>解密</para>
        /// </summary>
        /// <param name="encrypted">待解密数据</param>
        /// <returns>
        ///   <para>解密后的数据</para>
        /// </returns>
        byte[] Decrypt(byte[] encrypted);
    }
}