namespace Verve.Crypto
{
    /// <summary>
    /// 加解密接口
    /// </summary>
    public interface ICrypto
    {
        /// <summary> 加密 </summary>
        byte[] Encrypt(byte[] data);
        /// <summary> 解密 </summary>
        byte[] Decrypt(byte[] encrypted);
    }
}