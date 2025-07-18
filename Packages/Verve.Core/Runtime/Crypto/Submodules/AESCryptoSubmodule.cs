namespace Verve.Crypto
{
    using System;
    using System.Text;
    using Application;
    using System.Security.Cryptography;
    
    
    /// <summary>
    /// AES对称加密子模块
    /// </summary>
    [System.Serializable]
    public partial class AESCryptoSubmodule : ICryptoSubmodule
    {
        protected byte[] m_EncryptionKey;
        
        public string ModuleName => "AESCrypto";
        
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            var app = dependencies.Get<ApplicationFeature>();
            
            m_EncryptionKey = GenerateDeviceKey(
                app.Current.DeviceId
            );
        }

        public virtual void OnModuleUnloaded()
        {
            
        }

        public virtual byte[] Encrypt(byte[] data)
        {
            using (Aes aes = CreateAes())
            {
                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }
        
        public virtual byte[] Decrypt(byte[] encrypted)
        {
            using (Aes aes = CreateAes())
            {
                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
            }
        }

        private Aes CreateAes()
        {
            Aes aes = Aes.Create();
            aes.Key = m_EncryptionKey;
            aes.IV = new byte[16];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
        
        protected byte[] GenerateDeviceKey(string deviceId)
        {
            using (SHA256 sha = SHA256.Create())
            {
                string composite = $"{deviceId}";
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(composite));
                
                byte[] key = new byte[32];
                Buffer.BlockCopy(hash, 0, key, 0, 32);
                return key;
            }
        }
    }
}
