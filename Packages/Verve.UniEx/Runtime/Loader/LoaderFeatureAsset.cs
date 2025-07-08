#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Loader
{
    using Verve;
    using UnityEngine;

    
    /// <summary>
    /// 加载器功能数据
    /// </summary>
    public partial class LoaderFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Loader";
        
        public override string FeatureName => m_FeatureName;


        public override IGameFeature CreateFeature()
        {
            return new LoaderFeature();
        }
    }
}

#endif