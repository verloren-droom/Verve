#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Crypto
{
    using Verve.Crypto;

    
    /// <summary>
    /// 加解密子模块基类
    /// </summary>
    public abstract class CryptoSubmodule: GameFeatureSubmodule<CryptoGameFeatureComponent>, ICrypto
    {
        public abstract byte[] Encrypt(byte[] data);
        public abstract byte[] Decrypt(byte[] encrypted);
    }
}

#endif