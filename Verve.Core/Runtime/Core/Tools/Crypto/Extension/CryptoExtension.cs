namespace Verve
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///   <para>加解密扩展。</para>
    /// </summary>
    public static class CryptoExtension
    {
        /// <summary>
        ///   <para>加密字符串。</para>
        /// </summary>
        /// <param name="plainText">明文。</param>
        /// <param name="encoding">编码。</param>
        /// <param name="key">借用的密钥。</param>
        /// <param name="self">加解密工具。</param>
        public static string Encrypt(this ICrypto self, string plainText, byte[] key, Encoding encoding = null)
        {
            byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(plainText);
            byte[] encrypted = self.Encrypt(bytes, key);
            return Convert.ToBase64String(encrypted);
        }
        
        /// <summary>
        ///   <para>解密字符串。</para>
        /// </summary>
        /// <param name="encryptedText">密文。</param>
        /// <param name="encoding">编码。</param>
        /// <param name="key">借用的密钥。</param>
        /// <param name="self">加解密工具。</param>
        public static string Decrypt(this ICrypto self, string encryptedText, byte[] key, Encoding encoding = null)
        {
            byte[] encrypted = Convert.FromBase64String(encryptedText);
            byte[] decrypted = self.Decrypt(encrypted, key);
            return (encoding ?? Encoding.UTF8).GetString(decrypted);
        }

        /// <summary>
        ///   <para>将字节数组加密并转换为 Base64 字符串。</para>
        /// </summary>
        /// <param name="data">字节数组。</param>
        /// <param name="key">借用的密钥。</param>
        /// <param name="self">加解密工具。</param>
        public static string EncryptToBase64(this ICrypto self, byte[] data, byte[] key)
            => Convert.ToBase64String(self.Encrypt(data, key));
        
        /// <summary>
        ///   <para>从 Base64 字符串解密为字节数组。</para>
        /// </summary>
        /// <param name="base64String">密文。</param>
        /// <param name="key">借用的密钥。</param>
        /// <param name="self">加解密工具。</param>
        public static byte[] DecryptFromBase64(this ICrypto self, string base64String, byte[] key)
            => self.Decrypt(Convert.FromBase64String(base64String), key);
        
        /// <summary>
        ///   <para>加密流数据。</para>
        /// </summary>
        /// <param name="input">借用的输入流；读取当前位置到末尾。</param>
        /// <param name="key">借用的密钥。</param>
        /// <param name="self">加解密工具。</param>
        public static async Task<byte[]> EncryptAsync(this ICrypto self, Stream input, byte[] key)
        {
            using var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream);
            return self.Encrypt(memoryStream.ToArray(), key);
        }
        
        /// <summary>
        ///   <para>解密流数据。</para>
        /// </summary>
        /// <param name="input">借用的输入流；读取当前位置到末尾。</param>
        /// <param name="key">借用的密钥。</param>
        /// <param name="self">加解密工具。</param>
        public static async Task<byte[]> DecryptAsync(this ICrypto self, Stream input, byte[] key)
        {
            using var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream);
            return self.Decrypt(memoryStream.ToArray(), key);
        }
    }
}