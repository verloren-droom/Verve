#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Storage
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 持久化功能数据
    /// </summary>
    [System.Serializable]
    public partial class StorageFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Storage";
        
        public override string FeatureName => m_FeatureName;

        public override IReadOnlyList<string> Dependencies => new string[] { "VerveUniEx.File" };


        public override IGameFeature CreateFeature()
        {
            return new StorageFeature();
        }
    }
}

#endif