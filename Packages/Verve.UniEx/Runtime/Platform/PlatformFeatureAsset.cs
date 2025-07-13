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
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Platform";
        
        public override string FeatureName => m_FeatureName;


        public override IGameFeature CreateFeature()
        {
            return new PlatformFeature();
        }
    }
}

#endif