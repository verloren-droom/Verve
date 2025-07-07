#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Platform
{
    using Verve;
    using UnityEngine;
    
    
    /// <summary>
    /// 平台功能数据
    /// </summary>
    [CreateAssetMenu(fileName = "New PlatformFeature", menuName = "Verve/Features/PlatformFeature", order = 0)]
    public class PlatformFeatureAsset : GameFeatureAsset
    {
        public override string FeatureName => "VerveUniEx.Platform";
        
        
        public override IGameFeature CreateFeature()
        {
            return new PlatformFeature();
        }
    }
}

#endif