namespace Verve.Crypto
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    
    /// <summary>
    ///   <para>加密解密扩展类</para>
    /// </summary>
    public static class CryptoExtension
    {
        /// <summary>
        ///   <para>加密字符串</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="encoding">编码</param>
        /// <returns>
        ///   <para>密文</para>
        /// </returns>
        public static string Encrypt(this ICrypto self, string plainText, Encoding encoding = null)
        {
            byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(plainText);
            byte[] encrypted = self.Encrypt(bytes);
            return Convert.ToBase64String(encrypted);
        }
        
        /// <summary>
        ///   <para>解密字符串</para>
        /// </summary>
        /// <param name="encryptedText">密文</param>
        /// <param name="encoding">编码</param>
        /// <returns>
        ///   <para>明文</para>
        /// </returns>
        public static string Decrypt(this ICrypto self, string encryptedText, Encoding encoding = null)
        {
            byte[] encrypted = Convert.FromBase64String(encryptedText);
            byte[] decrypted = self.Decrypt(encrypted);
            return (encoding ?? Encoding.UTF8).GetString(decrypted);
        }
        
        /// <summary>
        ///   <para>异步加密字符串</para>
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="encoding">编码</param>
        /// <returns>
        ///   <para>密文</para>
        /// </returns>
        public static async Task<string> EncryptAsync(this ICrypto self, string plainText, Encoding encoding = null)
        {
            return await Task.Run(() => self.Encrypt(plainText, encoding));
        }

        /// <summary>
        ///   <para>异步解密字符串</para>
        /// </summary>
        /// <param name="encryptedText">密文</param>
        /// <param name="encoding">编码</param>
        /// <returns>
        ///   <para>明文</para>
        /// </returns>
        public static async Task<string> DecryptAsync(this ICrypto self, string encryptedText, Encoding encoding = null)
        {
            return await Task.Run(() => self.Decrypt(encryptedText, encoding));
        }
        
        /// <summary>
        ///   <para>将字节数组加密并转换为Base64字符串</para>
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <returns>
        ///   <para>密文</para>
        /// </returns>
        public static string EncryptToBase64(this ICrypto self, byte[] data)
        {
            return Convert.ToBase64String(self.Encrypt(data));
        }
        
        /// <summary>
        ///   <para>从Base64字符串解密为字节数组</para>
        /// </summary>
        /// <param name="base64String">密文</param>
        /// <returns>
        ///   <para>字节数组</para>
        /// </returns>
        public static byte[] DecryptFromBase64(this ICrypto self, string base64String)
        {
            return self.Decrypt(Convert.FromBase64String(base64String));
        }
        
        /// <summary>
        ///   <para>加密流数据</para>
        /// </summary>
        /// <param name="input">输入流</param>
        public static async Task<byte[]> EncryptAsync(this ICrypto self, Stream input)
        {
            using var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream);
            return self.Encrypt(memoryStream.ToArray());
        }
        
        /// <summary>
        ///   <para>解密流数据</para>
        /// </summary>
        /// <param name="input">输入流</param>
        public static async Task<byte[]> DecryptAsync(this ICrypto self, Stream input)
        {
            using var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream);
            return self.Decrypt(memoryStream.ToArray());
        }
    }
}