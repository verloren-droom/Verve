#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using Verve;
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///  <para>游戏流程节点</para>
    /// </summary>
    [Serializable]
    public abstract class GameFlowNode : IGameFlowNode, ISerializationCallbackReceiver
    {
        [SerializeField, Tooltip("游戏流程节点ID"), ReadOnly] private string m_NodeID;
        
        public string NodeID => m_NodeID;
        public virtual bool IsCompleted { get; protected set; }
        public virtual bool IsExecuting { get; protected set; }

        private static string GenerateNodeID() => $"node_{Guid.NewGuid().ToString("N")[..32]}";
        
        protected GameFlowNode(string actionID = null)
        {
            m_NodeID = actionID ?? GenerateNodeID();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async Task IGameFlowNode.Execute(CancellationToken ct)
        {
            if (IsExecuting || IsCompleted)
                return;
            
            IsExecuting = true;
            IsCompleted = false;
            
            await OnExecute(ct);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IGameFlowNode.Cancel()
        {
            if (!IsExecuting || IsCompleted)
                return;
            
            IsExecuting = false;
            OnCancel();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IGameFlowNode.Reset()
        {
            IsExecuting = false;
            IsCompleted = false;
            OnReset();
        }

        /// <summary>
        ///  <para>执行</para>
        /// </summary>
        protected abstract Task OnExecute(CancellationToken ct);
        /// <summary>
        ///  <para>取消</para>
        /// </summary>
        protected virtual void OnCancel() { }
        /// <summary>
        ///  <para>重置</para>
        /// </summary>
        protected virtual void OnReset() { }
        
        /// <summary>
        ///  <para>标记为完成</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void MarkCompleted()
        {
            IsExecuting = false;
            IsCompleted = true;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(m_NodeID))
            {
                m_NodeID = GenerateNodeID();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // if (string.IsNullOrEmpty(m_NodeID))
            // {
            //     m_NodeID = GenerateNodeID();
            // }
        }
    }
}

#endif