#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.MVC
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// MVC功能数据
    /// </summary>
    public partial class MVCFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.MVC";
        [SerializeField, Tooltip("视图画布预制体（必选）"), RequireComponentOnGameObject(typeof(MVCFeatureComponent))] private GameObject m_ViewPrefab;
        
        public override string FeatureName => m_FeatureName;

        public override IReadOnlyList<string> Dependencies => new string[] { "VerveUniEx.Loader" };


        public override IGameFeature CreateFeature()
        {
            return this.CreateFeatureInstance<MVCFeatureComponent>(m_ViewPrefab);
        }
    }
}

#endif