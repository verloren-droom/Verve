#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Timer
{
    using Verve;
    using UnityEngine;
    
    
    public partial class TimerFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Timer";
        
        public override string FeatureName => m_FeatureName;
        
        
        public override IGameFeature CreateFeature()
        {
            var timer = new Verve.Timer.TimerFeatureData();
            GameFeaturesSystem.Runtime.RegisterFeatureData(timer);
            GameFeaturesSystem.Runtime.LoadFeature(timer.FeatureName);
            GameFeaturesSystem.Runtime.ActivateFeature(timer.FeatureName);
            return this.CreateFeatureInstance<TimerFeatureComponent>();
        }
    }
}

#endif