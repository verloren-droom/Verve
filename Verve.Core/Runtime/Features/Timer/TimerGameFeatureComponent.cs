#if UNITY_5_3_OR_NEWER

namespace Verve.Timer
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>时间游戏功能组件</para>
    /// </summary>
    [Serializable, GameFeatureComponentMenu("Verve/Timer")]
    public sealed class TimerGameFeatureComponent : GameFeatureComponent
    {
        [SerializeField, Tooltip("最大回溯时间（秒）")] private ClampedFloatParameter m_MaxRewindTime = new ClampedFloatParameter(10f, 0f, 60f);
        [SerializeField, Tooltip("回溯采样频率（每秒采样次数）")] private GameFeatureParameter<float> m_SampleFrequency = new GameFeatureParameter<float>(10f);
        [SerializeField, Tooltip("每帧最大回溯对象数量")] private GameFeatureParameter<int> m_MaxRewindsPerFrame = new GameFeatureParameter<int>(50);
        
        
        /// <summary>
        ///   <para>最大回溯时间（秒）</para>
        /// </summary>
        public float MaxRewindTime => m_MaxRewindTime.Value;
        
        /// <summary>
        ///   <para>回溯采样频率（每秒采样次数）</para>
        /// </summary>
        public float SampleFrequency => m_SampleFrequency.Value;
        
        /// <summary>
        ///   <para>每帧最大回溯对象数量</para>
        /// </summary>
        public int MaxRewindsPerFrame => m_MaxRewindsPerFrame.Value;
    }
}

#endif