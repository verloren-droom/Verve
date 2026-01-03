namespace Verve
{
    using System.Text;
    using System.Security.Cryptography;


    /// <summary>
    ///   <para>AES加解密（对称加密）</para>
    /// </summary>
    internal sealed class AESCrypto : InstanceBase<AESCrypto>, ICrypto
    {
        private const string KEY = "ABCD-EFGH-IJKL-MNOP";
        
        public byte[] Encrypt(byte[] data)
        {
            using var aes = CreateAes();
            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }
        
        public byte[] Decrypt(byte[] encrypted)
        {
            using var aes = CreateAes();
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        /// <summary>
        ///   <para>创建AES对象</para>
        /// </summary>
        /// <returns>
        ///   <para>AES对象实例</para>
        /// </returns>
        private Aes CreateAes(Encoding encoding = null)
        {
            var aes = Aes.Create();
            aes.Key = (encoding ?? Encoding.UTF8).GetBytes(KEY);
            aes.IV = new byte[16];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
    }
}