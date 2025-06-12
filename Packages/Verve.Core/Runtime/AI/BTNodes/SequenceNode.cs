namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct SequenceNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        [NotNull] public IBTNode[] Children;
    }
    

    /// <summary>
    /// 顺序节点（按顺序执行所有子节点）
    /// </summary>
    [Serializable]
    public struct SequenceNode : ICompositeNode, IResetableNode
    {
        public SequenceNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        /// <summary> 当前节点索引值 </summary>
        private int m_CurrentChildIndex;

        public readonly int CurrentChildIndex => m_CurrentChildIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (ChildCount <= 0) return NodeStatus.Failure;

            while (m_CurrentChildIndex < Data.Children.Length)
            {
                LastStatus = this.RunChildNode(ref Data.Children[m_CurrentChildIndex], ref ctx);
        
                if (LastStatus == NodeStatus.Running) 
                    return NodeStatus.Running;
            
                if (LastStatus == NodeStatus.Failure)
                {
                    m_CurrentChildIndex = 0;
                    return NodeStatus.Failure;
                }
        
                m_CurrentChildIndex++;
            }
    
            m_CurrentChildIndex = 0;
            return NodeStatus.Success;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            for (int i = 0; i < Data.Children.Length; i++)
            {
                if (Data.Children[i] is IResetableNode resetable)
                    resetable.Reset(ref ctx);
            }
        }

        #endregion

        #region 复合节点

        public int ChildCount => Data.Children?.Length ?? 0;
        
        
        public IEnumerable<IBTNode> GetChildren() => Data.Children;
        
        public IEnumerable<IBTNode> GetActiveChildren()
        {
            if (m_CurrentChildIndex < ChildCount)
                yield return Data.Children[m_CurrentChildIndex];
        }

        #endregion
    }
}