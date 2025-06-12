namespace Verve.AI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    [Serializable]
    public struct ParallelNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        /// <summary> 允许所有子节点成功 </summary>
        public bool RequireAllSuccess;
    }
    
    
    /// <summary>
    /// 并行节点（同时执行所有子节点）
    /// </summary>
    [Serializable]
    public struct ParallelNode : ICompositeNode, IResetableNode
    {
        public ParallelNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        private NodeStatus[] m_ChildStatus;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (m_ChildStatus == null || m_ChildStatus.Length != Data.Children.Length)
                m_ChildStatus = new NodeStatus[Data.Children.Length];
            
            int successCount = 0;
            int runningCount = 0;
            
            for (int i = 0; i < Data.Children.Length; i++)
            {
                if (m_ChildStatus[i] == NodeStatus.Running)
                {
                    m_ChildStatus[i] = this.RunChildNode(ref Data.Children[i], ref ctx);
                }
                
                if (m_ChildStatus[i] == NodeStatus.Success) successCount++;
                if (m_ChildStatus[i] == NodeStatus.Running) runningCount++;
                if (m_ChildStatus[i] == NodeStatus.Failure && Data.RequireAllSuccess)
                    return NodeStatus.Failure;
            }
            
            if (runningCount > 0) 
                return NodeStatus.Running;
            
            return successCount > 0 ? NodeStatus.Success : NodeStatus.Failure;
        }

        #region 可重置节点

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            if (m_ChildStatus == null) return;
            Array.Clear(m_ChildStatus, 0, m_ChildStatus.Length);
            for (int i = 0; i < Data.Children.Length; i++)
            {
                if (Data.Children[i] is IResetableNode resetable)
                    resetable.Reset(ref ctx);
            }
        }

        #endregion

        #region 复合节点

        public int ChildCount => Data.Children?.Length ?? 0;
        
        
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => Data.Children;
        
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            for (int i = 0; i < ChildCount; i++)
            {
                if (m_ChildStatus[i] == NodeStatus.Running)
                    yield return Data.Children[i];
            }
        }

        #endregion
    }
}