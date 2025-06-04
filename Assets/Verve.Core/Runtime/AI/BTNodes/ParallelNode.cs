namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 并行节点（同时执行所有子节点）
    /// </summary>
    [Serializable]
    public struct ParallelNode : IBTNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        /// <summary> 允许所有子节点成功 </summary>
        public bool RequireAllSuccess;
        
        private NodeStatus[] m_ChildStatus;
    
        
        public NodeStatus Run(ref Blackboard bb, float deltaTime)
        {
            if (m_ChildStatus == null || m_ChildStatus.Length != Children.Length)
                m_ChildStatus = new NodeStatus[Children.Length];
            
            int successCount = 0;
            int runningCount = 0;
            
            for (int i = 0; i < Children.Length; i++)
            {
                if (m_ChildStatus[i] == NodeStatus.Running)
                {
                    m_ChildStatus[i] = Children[i].Run(ref bb, deltaTime);
                }
                
                if (m_ChildStatus[i] == NodeStatus.Success) successCount++;
                if (m_ChildStatus[i] == NodeStatus.Running) runningCount++;
                if (m_ChildStatus[i] == NodeStatus.Failure && RequireAllSuccess)
                    return NodeStatus.Failure;
            }
            
            if (runningCount > 0) 
                return NodeStatus.Running;
                
            return successCount > 0 ? NodeStatus.Success : NodeStatus.Failure;
        }
        
        void IResetableNode.Reset()
        {
            m_ChildStatus = null;
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }
}