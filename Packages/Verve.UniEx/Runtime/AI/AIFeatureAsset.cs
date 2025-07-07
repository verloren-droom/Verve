#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using Verve;
    using UnityEngine;
    
    
    [CreateAssetMenu(fileName = "New AIFeature", menuName = "Verve/Features/AIFeature", order = 0)]
    public partial class AIFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.AI";
        [SerializeField, Tooltip("AI预制体（可选）"), RequireComponentOnGameObject(typeof(AIFeatureComponent))] private GameObject m_AIPrefab;

        public override string FeatureName => m_FeatureName;
        
        
        public override IGameFeature CreateFeature()
        {
            return this.CreateFeatureInstance<AIFeatureComponent>(m_AIPrefab);
        }
    }
}

#endif