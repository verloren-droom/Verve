namespace Verve.Crypto
{
    /// <summary>
    /// 加解密子模块接口
    /// </summary>
    public interface ICryptoSubmodule : IGameFeatureSubmodule
    {
        /// <summary> 加密 </summary>
        byte[] Encrypt(byte[] data);
        /// <summary> 解密 </summary>
        byte[] Decrypt(byte[] encrypted);
    }
}