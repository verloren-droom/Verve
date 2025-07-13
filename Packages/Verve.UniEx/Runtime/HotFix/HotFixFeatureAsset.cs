#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.HotFix
{
    using Verve;
    using UnityEngine;
    using Loader;
    using System.Collections.Generic;
    
    
    public partial class HotFixFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.HotFix";
        [SerializeField, Tooltip("服务器地址")] private string m_ServerUrl = Verve.HotFix.HotFixFeatureData.ServerUrl;
        
        public override string FeatureName => m_FeatureName;

        public override IReadOnlyCollection<string> Dependencies => new string[] { "VerveUniEx.Loader", "VerveUniEx.Serializable", "VerveUniEx.Platform", "VerveUniEx.Net" };

        public override IGameFeature CreateFeature() => new HotFixFeature();
    }
}

#endif