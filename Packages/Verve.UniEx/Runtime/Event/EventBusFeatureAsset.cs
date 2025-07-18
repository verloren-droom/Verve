#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Event
{
    using Verve;
    using UnityEngine;
    using Verve.Event;
    
    
    /// <summary>
    /// 事件总线功能数据
    /// </summary>
    public partial class EventBusFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Event";
        
        public override string FeatureName => m_FeatureName;
        
        
        public override IGameFeature CreateFeature() => new EventBusFeature();
    }
}

#endif