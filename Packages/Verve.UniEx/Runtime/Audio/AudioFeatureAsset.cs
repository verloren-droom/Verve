#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Audio
{
    using Verve;
    using UnityEngine;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 音频功能数据
    /// </summary>
    public partial class AudioFeatureAsset : GameFeatureAsset
    {
        [SerializeField, Tooltip("功能名称（保证全局唯一）"), ReadOnly] private string m_FeatureName = "VerveUniEx.Audio";
        [SerializeField, Tooltip("声音预制体（可选）"), RequireComponentOnGameObject(typeof(AudioFeatureComponent))] private GameObject m_AudioPrefab;

        public override IReadOnlyCollection<string> Dependencies => new string[] { };

        public override string FeatureName => m_FeatureName;
        

        public override IGameFeature CreateFeature()
        {
            return this.CreateFeatureInstance<AudioFeatureComponent>(m_AudioPrefab);
        }
    }
}

#endif