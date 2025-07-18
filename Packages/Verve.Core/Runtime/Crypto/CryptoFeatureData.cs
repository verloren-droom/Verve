namespace Verve.Crypto
{
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 加解密功能数据
    /// </summary>
    [System.Serializable]
    public class CryptoFeatureData : GameFeatureData
    {
        public override IReadOnlyCollection<string> Dependencies => new string[] { "Verve.Application" };
        public override IGameFeature CreateFeature() => new CryptoFeature();
    }
}