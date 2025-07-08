#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Platform
{
    using Verve;
    using UnityEngine;
    
    
    /// <summary>
    /// 平台功能数据
    /// </summary>
    public partial class PlatformFeatureAsset : GameFeatureAsset
    {
        public override string FeatureName => "VerveUniEx.Platform";
        
        
        public override IGameFeature CreateFeature()
        {
            return new PlatformFeature();
        }
    }
}

#endif