namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    

    /// <summary>
    /// 顺序节点（按顺序执行所有子节点）
    /// </summary>
    [Serializable]
    public struct SequenceNode : ICompositeNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        
        /// <summary> 当前节点索引值 </summary>
        private int m_CurrentChildIndex;

        public int CurrentChildIndex => m_CurrentChildIndex;
        
        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (ChildCount <= 0) return NodeStatus.Failure;

            while (m_CurrentChildIndex < Children.Length)
            {
                var status = Children[m_CurrentChildIndex].Run(ref ctx);
        
                if (status == NodeStatus.Running) 
                    return NodeStatus.Running;
            
                if (status == NodeStatus.Failure)
                {
                    m_CurrentChildIndex = 0;
                    return NodeStatus.Failure;
                }
        
                m_CurrentChildIndex++;
            }
    
            m_CurrentChildIndex = 0;
            return NodeStatus.Success;
        }
        
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] is IResetableNode resetable)
                    resetable.Reset(ref ctx);
            }
        }

        
        public int ChildCount => Children?.Length ?? 0;
        public IEnumerable<IBTNode> GetChildren() => Children;
        public IEnumerable<IBTNode> GetActiveChildren()
        {
            if (m_CurrentChildIndex < ChildCount)
                yield return Children[m_CurrentChildIndex];
        }
    }
}