#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Debugger
{
    using Verve;
    using UnityEngine;
    
    
    /// <summary>
    /// 调试器功能数据
    /// </summary>
    [CreateAssetMenu(fileName = "New DebuggerFeature", menuName = "Verve/Features/DebuggerFeature")]
    public class DebuggerFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Debugger";

        [SerializeField, Tooltip("调试器预制体（可选）")] private GameObject m_DebuggerPrefab;
        
        public override string FeatureName => m_FeatureName;


        public override IGameFeature CreateFeature()
        {
            return new VerveUniEx.Debugger.DebuggerFeature();
        }
    }
}

#endif