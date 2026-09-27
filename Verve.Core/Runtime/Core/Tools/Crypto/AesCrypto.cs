namespace Verve
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    ///   <para>AES 加密工具；使用 AES-256-CBC 和 HMAC-SHA256。</para>
    /// </summary>
    internal sealed class AesCrypto : ICrypto
    {
        /// <summary>
        ///   <para>初始化向量大小。</para>
        /// </summary>
        private const int IvSize = 16;
        /// <summary>
        ///   <para>认证标签字节数。</para>
        /// </summary>
        private const int TagSize = 32;
        
        /// <inheritdoc />
        public byte[] Encrypt(byte[] data, byte[] key)
        {
            ValidateKey(key);
            if (data == null) throw new ArgumentNullException(nameof(data));
            using var aes = CreateAes(key);
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
            var result = new byte[IvSize + encrypted.Length + TagSize];
            Buffer.BlockCopy(aes.IV, 0, result, 0, IvSize);
            Buffer.BlockCopy(encrypted, 0, result, IvSize, encrypted.Length);
            using var hmac = CreateHmac(key);
            var tag = hmac.ComputeHash(result, 0, result.Length - TagSize);
            Buffer.BlockCopy(tag, 0, result, result.Length - TagSize, TagSize);
            return result;
        }

        /// <inheritdoc />
        public byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            ValidateKey(key);
            if (encrypted == null) throw new ArgumentNullException(nameof(encrypted));
            if (encrypted.Length < IvSize * 2 + TagSize || (encrypted.Length - IvSize - TagSize) % IvSize != 0)
                throw new CryptographicException("Invalid encrypted payload length.");

            using var hmac = CreateHmac(key);
            var tag = hmac.ComputeHash(encrypted, 0, encrypted.Length - TagSize);
            int difference = 0;
            for (int i = 0; i < TagSize; i++) difference |= tag[i] ^ encrypted[encrypted.Length - TagSize + i];
            if (difference != 0)
                throw new CryptographicException("Encrypted payload authentication failed.");

            using var aes = CreateAes(key);
            var iv = new byte[IvSize];
            Buffer.BlockCopy(encrypted, 0, iv, 0, IvSize);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, IvSize, encrypted.Length - IvSize - TagSize);
        }

        /// <summary>
        ///   <para>校验密钥长度。</para>
        /// </summary>
        /// <param name="key">密钥。</param>
        private static void ValidateKey(byte[] key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length != 64) throw new ArgumentException("Expected a 64-byte random key.", nameof(key));
        }

        /// <summary>
        ///   <para>创建 AES；清理临时密钥副本。</para>
        /// </summary>
        /// <param name="key">密钥。</param>
        private static Aes CreateAes(byte[] key)
        {
            var aes = Aes.Create();
            var encryptionKey = key[..32];
            try
            {
                aes.Key = encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                return aes;
            }
            catch { aes.Dispose(); throw; }
            finally { Array.Clear(encryptionKey, 0, encryptionKey.Length); }
        }

        /// <summary>
        ///   <para>创建 HMAC；清理临时密钥副本。</para>
        /// </summary>
        /// <param name="key">密钥。</param>
        private static HMACSHA256 CreateHmac(byte[] key)
        {
            var authenticationKey = key[32..];
            try { return new HMACSHA256(authenticationKey); }
            finally { Array.Clear(authenticationKey, 0, authenticationKey.Length); }
        }
    }
}