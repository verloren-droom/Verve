#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Application
{
    using Verve;
    using UnityEngine;
    
    
    /// <summary>
    /// 应用功能数据
    /// </summary>
    public partial class ApplicationFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Application";
        
        public override string FeatureName => m_FeatureName;
        
        
        public override IGameFeature CreateFeature()
        {
            return new ApplicationFeature();
        }
    }
}

#endif