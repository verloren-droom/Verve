#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Crypto
{
    using Verve;
    using Application;
    
    
    /// <summary>
    /// AES加解密子模块 - 对称加密
    /// </summary>
    public class AESCryptoSubmodule : Verve.Crypto.AESCryptoSubmodule
    {
        public override void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            var app = dependencies.Get<ApplicationFeature>();

            m_EncryptionKey = GenerateDeviceKey(
                app.Current.DeviceId
            );
        }
    }
}

#endif