namespace Verve.Crypto
{
    using System;
    using System.Text;

    
    public static class CryptoSubmoduleExtension
    {
        /// <summary> 加密字符串 </summary>
        public static string Encrypt(this ICryptoSubmodule self, string plainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = self.Encrypt(bytes);
            return Convert.ToBase64String(encrypted);
        }
        
        /// <summary> 解密字符串 </summary>
        public static string Decrypt(this ICryptoSubmodule self, string encryptedText)
        {
            byte[] encrypted = Convert.FromBase64String(encryptedText);
            byte[] decrypted = self.Decrypt(encrypted);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}