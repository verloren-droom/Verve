#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    
    
    /// <summary>
    ///  <para>延时节点</para>
    /// </summary>
    [Serializable, GameFlowNode("Basic/Delay", "Delay", "延时节点")]
    public class DelayNode : GameFlowNode
    {
        [SerializeField, Tooltip("持续时间"), Min(0f)] private float m_Duration;
        [SerializeField, Tooltip("使用真实时间（不受Time.timeScale影响）")] private bool m_UseRealTime;

        public DelayNode() : base() {}

        public DelayNode(float duration, bool useRealTime = false, string nodeID = null) : base(nodeID)
        {
            m_Duration = duration;
            m_UseRealTime = useRealTime;
        }

        protected override async Task OnExecute(CancellationToken ct = default)
        {
            float elapsed = 0f;
        
            while (elapsed < m_Duration)
            {
                ct.ThrowIfCancellationRequested();
            
                float deltaTime = m_UseRealTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;
            
                await Task.Yield();
            }
        
            MarkCompleted();
        }
    }
}

#endif