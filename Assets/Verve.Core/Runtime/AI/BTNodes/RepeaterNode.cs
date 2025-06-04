namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 重复执行节点
    /// </summary>
    [Serializable]
    public struct RepeaterNode : IBTNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode Child;
        /// <summary> 循环次数 </summary>
        public int RepeatCount;
        
        private int m_CurrentCount;


        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
        {
            if (RepeatCount <= 0) return NodeStatus.Failure;
            if (m_CurrentCount >= RepeatCount)
                return NodeStatus.Success;

            var status = Child.Run(ref bb, deltaTime);
        
            if (status == NodeStatus.Running)
                return NodeStatus.Running;
            
            if (status == NodeStatus.Success)
                m_CurrentCount++;
            else
                return NodeStatus.Failure;

            return m_CurrentCount < RepeatCount ? NodeStatus.Running : NodeStatus.Success;
        }

        void IResetableNode.Reset()
        {
            m_CurrentCount = 0;
            if (Child is IResetableNode resetable)
                resetable.Reset();
        }
    }
}