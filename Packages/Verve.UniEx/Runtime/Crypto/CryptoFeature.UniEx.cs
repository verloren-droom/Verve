namespace VerveUniEx.Crypto
{
    using Verve;

    
    /// <summary>
    /// 加解密功能
    /// </summary>
    public class CryptoFeature : Verve.Crypto.CryptoFeature
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new AESCryptoSubmodule());
        }
    }
}