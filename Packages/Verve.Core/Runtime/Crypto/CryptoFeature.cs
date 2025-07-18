namespace Verve.Crypto
{
    /// <summary>
    /// 加解密功能
    /// </summary>
    [System.Serializable]
    public class CryptoFeature : ModularGameFeature<ICryptoSubmodule>
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new AESCryptoSubmodule());
        }
    }
}