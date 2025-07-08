#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Serializable
{
    using Verve;
    using UnityEngine;
    using Verve.Serializable;
    
    
    /// <summary>
    /// 序列化功能数据
    /// </summary>
    public partial class SerializableFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Serializable";
        
        public override string FeatureName => m_FeatureName;

        
        public override IGameFeature CreateFeature()
        {
            return new SerializableFeature();
        }
    }
}

#endif