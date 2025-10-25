#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    /// <summary>
    ///  <para>序列节点</para>
    /// </summary>
    [Serializable, GameFlowNode("Basic/Sequence", "Sequence", "序列节点")]
    public class SequenceNode : GameFlowNode, ICompositeGameFlowNode
    {
        [SerializeReference, Tooltip("节点列表"), HideInInspector] private List<IGameFlowNode> m_Nodes = new List<IGameFlowNode>();
        private IGameFlowNode m_CurrentNode;
        private CancellationTokenSource m_ExecutionCts;
        [SerializeField, Tooltip("当前索引值"), ReadOnly] private int m_CurrentIndex = -1;
        
        public IEnumerable<IGameFlowNode> Children => m_Nodes;
        
        public SequenceNode() : base() { }
        public SequenceNode(string actionID) : base(actionID) { }
        
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

        public void Append(IGameFlowNode node)
        {
            if (node == null) return;
            m_Nodes?.Add(node);
        }

        public void Insert(IGameFlowNode node, int insertIndex)
        {
            if (node == null) return;
            m_Nodes?.Insert(insertIndex, node);
        }
        
        public void Remove(IGameFlowNode node)
        {
            if (node == null) return;
            m_Nodes?.Remove(node);
        }
        
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < m_Nodes.Count)
                m_Nodes?.RemoveAt(index);
        }

        public void Clear()
        {
            m_Nodes?.Clear();
        }
    }
}

#endif