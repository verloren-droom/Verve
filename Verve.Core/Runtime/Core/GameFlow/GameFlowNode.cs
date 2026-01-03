#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using Verve;
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>游戏流程节点</para>
    /// </summary>
    [Serializable]
#if UNITY_2019_4_OR_NEWER
    public abstract class GameFlowNode : IGameFlowNode, ISerializationCallbackReceiver, ICloneable
#else
    public class GameFlowNode : IGameFlowNode, ISerializationCallbackReceiver, ICloneable
#endif
    {
        [SerializeField, Tooltip("游戏流程节点ID"), ReadOnly] private string m_NodeID;
        
        public string NodeID => m_NodeID;
        public virtual bool IsCompleted { get; protected set; }
        public virtual bool IsExecuting { get; protected set; }

        private static string GenerateNodeID() => $"node_{Guid.NewGuid().ToString("N")[..32]}";
        
        protected GameFlowNode(string nodeID = null)
        {
            m_NodeID = nodeID ?? GenerateNodeID();
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
        ///   <para>执行</para>
        /// </summary>
        /// <param name="ct">取消令牌</param>
#if UNITY_2019_4_OR_NEWER
        protected abstract Task OnExecute(CancellationToken ct);
#else
        protected virtual void OnExecute(CancellationToken ct) {}
#endif
        /// <summary>
        ///   <para>取消</para>
        /// </summary>
        protected virtual void OnCancel() { }
        /// <summary>
        ///   <para>重置</para>
        /// </summary>
        protected virtual void OnReset() { }
        
        /// <summary>
        ///   <para>标记为完成</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void MarkCompleted()
        {
            IsExecuting = false;
            IsCompleted = true;
        }

        /// <summary>
        ///   <para>克隆</para>
        /// </summary>
        /// <returns>
        ///   <para>克隆节点</para>
        /// </returns>
        public GameFlowNode Clone()
        {
            var clonedNode = Activator.CreateInstance(GetType()) as GameFlowNode;
            
            var fields = typeof(GameFlowNode).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name != nameof(m_NodeID))
                {
                    field.SetValue(clonedNode, field.GetValue(this));
                }
            }
            
            clonedNode.m_NodeID = GenerateNodeID();
            
            return clonedNode;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}

#endif