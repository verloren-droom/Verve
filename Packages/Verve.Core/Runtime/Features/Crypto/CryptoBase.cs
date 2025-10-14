namespace Verve.Crypto
{
    /// <summary>
    /// 加解密基类
    /// </summary>
    public abstract class CryptoBase : ICrypto
    {
        public abstract byte[] Encrypt(byte[] data);
        public abstract byte[] Decrypt(byte[] encrypted);
    }
}