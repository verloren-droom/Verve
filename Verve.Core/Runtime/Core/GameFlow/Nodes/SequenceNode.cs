#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///    <para>序列节点</para>
    /// </summary>
    [Serializable, GameFlowNode("Basic/Sequence", "Sequence", "序列节点")]
    public sealed class SequenceNode : GameFlowNode, ICompositeGameFlowNode
    {
#if UNITY_2019_4_OR_NEWER
        [SerializeReference, Tooltip("节点列表"), HideInInspector] private List<IGameFlowNode> m_Nodes = new List<IGameFlowNode>();
#else
        [SerializeField, Tooltip("节点列表"), HideInInspector] private List<GameFlowNode> m_Nodes = new List<GameFlowNode>();
#endif
        [NonSerialized] private IGameFlowNode m_CurrentNode;
        [NonSerialized] private CancellationTokenSource m_ExecutionCts;
        [SerializeField, Tooltip("当前索引值"), ReadOnly] private int m_CurrentIndex = -1;
        
        public IEnumerable<IGameFlowNode> Children => m_Nodes;
        
        public SequenceNode() : base() { }
        public SequenceNode(string nodeID) : base(nodeID) { }
        
        protected override async Task OnExecute(CancellationToken ct = default)
        {
            if (m_Nodes == null || m_Nodes.Count <= 0)
            {
                MarkCompleted();
                return;
            }
            
            m_ExecutionCts?.Dispose();
            m_ExecutionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            
            int startIndex = m_CurrentIndex >= 0 ? m_CurrentIndex : 0;
            
            for (m_CurrentIndex = startIndex; m_CurrentIndex < m_Nodes.Count; m_CurrentIndex++)
            {
                m_ExecutionCts.Token.ThrowIfCancellationRequested();
                
                m_CurrentNode = m_Nodes[m_CurrentIndex];
                await m_CurrentNode.Execute(m_ExecutionCts.Token);
                m_CurrentNode = null;
            }
            
            m_CurrentIndex = -1;
            MarkCompleted();
        }

        protected override void OnCancel()
        {
            m_ExecutionCts?.Cancel();
            m_CurrentNode?.Cancel();
            m_CurrentNode = null;
        }

        protected override void OnReset()
        {
            m_ExecutionCts?.Dispose();
            m_ExecutionCts = null;
            
            m_CurrentNode?.Reset();
            m_CurrentNode = null;
            m_CurrentIndex = -1;
            
            foreach (var node in m_Nodes)
            {
                node?.Reset();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICompositeGameFlowNode.Append(IGameFlowNode node)
        {
            if (node == null) return;
            m_Nodes?.Add(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICompositeGameFlowNode.Insert(IGameFlowNode node, int insertIndex)
        {
            if (node == null) return;
            m_Nodes?.Insert(insertIndex, node);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICompositeGameFlowNode.Remove(IGameFlowNode node)
        {
            if (node == null) return;
            m_Nodes?.Remove(node);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICompositeGameFlowNode.RemoveAt(int index)
        {
            if (index >= 0 && index < m_Nodes.Count)
                m_Nodes?.RemoveAt(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICompositeGameFlowNode.Clear()
        {
            m_Nodes?.Clear();
        }
    }
}

#endif