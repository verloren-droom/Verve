#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Input
{
    using Verve;
    using UnityEngine;

    
    /// <summary>
    /// 输入功能数据
    /// </summary>
    [CreateAssetMenu(fileName = "New InputFeature", menuName = "Verve/Features/InputFeature", order = 0)]
    public partial class InputFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Input";
        
        public override string FeatureName => m_FeatureName;


        public override IGameFeature CreateFeature()
        {
            return new InputFeature();
        }
    }
}

#endif