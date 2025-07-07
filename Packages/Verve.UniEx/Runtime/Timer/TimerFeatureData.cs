using Verve;

#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Timer
{
    using UnityEngine;
    
    
    [CreateAssetMenu(fileName = "New TimerFeature", menuName = "Verve/Features/TimerFeature")]
    public partial class TimerFeatureData : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Timer";
        
        public override string FeatureName => m_FeatureName;
        public override IGameFeature CreateFeature()
        {
            Verve.GameFeaturesSystem.Runtime.RegisterFeatureData(new Verve.Timer.TimerFeatureData());
            return this.CreateFeatureInstance<TimerFeatureComponent>();
        }
    }
}

#endif