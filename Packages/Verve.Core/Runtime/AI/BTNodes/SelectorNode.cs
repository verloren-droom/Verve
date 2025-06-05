namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 选择节点（顺序执行子节点，直到某个子节点成功）
    /// </summary>
    [Serializable]
    public struct SelectorNode : IBTNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        
        private int m_CurrentChildIndex;
        
        public int CurrentChildIndex => m_CurrentChildIndex;
    
        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            for (int i = m_CurrentChildIndex; i < Children.Length; i++)
            {
                var status = Children[i].Run(ref ctx);
                if (status == NodeStatus.Running)
                {
                    m_CurrentChildIndex = i;
                    return NodeStatus.Running;
                }
                if (status == NodeStatus.Success)
                {
                    return NodeStatus.Success;
                }
            }
            return NodeStatus.Failure;
        }
    
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            foreach (var child in Children)
            {
                if (child is IResetableNode resetable)
                {
                    resetable.Reset(ref ctx);
                }
            }
        }
    }

}