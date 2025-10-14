namespace Verve.Crypto
{
    using System;
    using System.Text;

    
    /// <summary>
    /// 加密解密扩展类
    /// </summary>
    public static class CryptoExtension
    {
        /// <summary> 加密字符串 </summary>
        public static string Encrypt(this ICrypto self, string plainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = self.Encrypt(bytes);
            return Convert.ToBase64String(encrypted);
        }
        
        /// <summary> 解密字符串 </summary>
        public static string Decrypt(this ICrypto self, string encryptedText)
        {
            byte[] encrypted = Convert.FromBase64String(encryptedText);
            byte[] decrypted = self.Decrypt(encrypted);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}