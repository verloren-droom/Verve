#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Net
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    public partial class NetworkFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Net";
        
        public override string FeatureName => m_FeatureName;

        public override IReadOnlyCollection<string> Dependencies => new string[] { };
        
        public override IGameFeature CreateFeature() => new NetworkFeature();
    }
}

#endif