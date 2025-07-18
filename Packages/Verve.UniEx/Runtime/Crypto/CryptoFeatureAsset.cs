#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Crypto
{
    using Verve;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 加解密功能资源
    /// </summary>
    public partial class CryptoFeatureAsset : GameFeatureAsset
    {
        public override IReadOnlyCollection<string> Dependencies => new string[] { "VerveUniEx.Application" };
        public override IGameFeature CreateFeature() => new CryptoFeature();
    }
}

#endif