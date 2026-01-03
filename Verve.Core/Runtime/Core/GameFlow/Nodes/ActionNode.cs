#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Threading;
    using UnityEngine.Events;
    using System.Threading.Tasks;
    
    
    /// <summary>
    ///   <para>行为节点</para>
    /// </summary>
    [Serializable, GameFlowNode("Basic/Action", "Action", "行为节点")]
    public sealed class ActionNode : GameFlowNode
    {
        [SerializeField, Tooltip("回调事件")] private UnityEvent m_SerializedCallback;
        private readonly Action m_RuntimeCallback;
        
        public ActionNode() : base() { }
        
        public ActionNode(Action callback, string nodeID = null) : base(nodeID)
        {
            m_RuntimeCallback = callback;
            m_SerializedCallback = null;
        }
        
        protected override async Task OnExecute(CancellationToken ct = default)
        {
            m_RuntimeCallback?.Invoke();
        
            if (m_RuntimeCallback == null && m_SerializedCallback != null)
            {
                m_SerializedCallback.Invoke();
            }

            await Task.CompletedTask;
            MarkCompleted();
        }
    }
}

#endif