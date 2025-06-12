namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    [Serializable]
    public struct SelectorNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
    }
    
    
    /// <summary>
    /// 选择节点（顺序执行子节点，直到某个子节点成功）
    /// </summary>
    [Serializable]
    public struct SelectorNode : ICompositeNode, IResetableNode
    {
        public SelectorNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        private int m_CurrentChildIndex;
        
        public readonly int CurrentChildIndex => m_CurrentChildIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (ChildCount <= 0) return NodeStatus.Failure;
            
            for (int i = m_CurrentChildIndex; i < Data.Children.Length; i++)
            {
                LastStatus = this.RunChildNode(ref Data.Children[i], ref ctx);
                
                if (LastStatus == NodeStatus.Running)
                {
                    m_CurrentChildIndex = i;
                    return NodeStatus.Running;
                }
                if (LastStatus == NodeStatus.Success)
                {
                    return NodeStatus.Success;
                }
            }
            return NodeStatus.Failure;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            foreach (var child in Data.Children)
            {
                if (child is IResetableNode resetable)
                {
                    resetable.Reset(ref ctx);
                }
            }
        }

        #endregion

        #region 复合节点

        public int ChildCount => Data.Children?.Length ?? 0;
        
        
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => Data.Children;
        
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            if (m_CurrentChildIndex < ChildCount)
                yield return Data.Children[m_CurrentChildIndex];
        }

        #endregion
    }

}