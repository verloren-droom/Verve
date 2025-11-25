#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Crypto
{
    using System;
    using System.Text;
    using System.Security.Cryptography;


    /// <summary>
    ///   <para>AES加解密子模块</para>
    ///   <para>对称加密</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(CryptoGameFeature), Description = "AES加解密子模块 - 对称加密")]
    public sealed class AESCrypto : CryptoSubmodule
    {
        public override byte[] Encrypt(byte[] data)
        {
            using (Aes aes = CreateAes())
            {
                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }
        
        public override byte[] Decrypt(byte[] encrypted)
        {
            using (Aes aes = CreateAes())
            {
                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
            }
        }

        /// <summary>
        ///   <para>创建AES对象</para>
        /// </summary>
        /// <returns>
        ///   <para>AES对象实例</para>
        /// </returns>
        private Aes CreateAes()
        {
            Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Component.EncryptionKey);
            aes.IV = new byte[16];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
    }
}

#endif