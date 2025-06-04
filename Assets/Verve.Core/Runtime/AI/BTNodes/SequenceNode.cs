namespace Verve.AI
{
    using System;
    

    /// <summary>
    /// 顺序节点数据
    /// </summary>
    [Serializable]
    public struct SequenceData : INodeData
    {
        public IBTNode[] Children;
        private int m_CurrentIndex;
        
        public int CurrentIndex => m_CurrentIndex;
        public void IncrementIndex() => m_CurrentIndex++;
        public void ResetIndex() => m_CurrentIndex = 0;
        public void Reset()
        {
            ResetIndex();
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }


    /// <summary>
    /// 顺序节点逻辑处理器
    /// </summary>
    public struct SequenceProcessor : INodeProcessor<SequenceData>
    {
        public NodeStatus Run(ref SequenceData data, ref Blackboard bb, float deltaTime)
        {
            while (data.CurrentIndex < data.Children.Length)
            {
                var status = data.Children[data.CurrentIndex].Run(ref bb, deltaTime);
                if (status == NodeStatus.Running) return status;
                if (status == NodeStatus.Failure)
                {
                    data.ResetIndex();
                    return NodeStatus.Failure;
                }
                data.IncrementIndex();
            }
            data.ResetIndex();
            return NodeStatus.Success;
        }
    
        public void Reset(ref SequenceData data) => data.Reset();
    }


    /// <summary>
    /// 顺序节点（按顺序执行所有子节点）
    /// </summary>
    [Serializable]
    // [Obsolete("Please use BTNode<SequenceData, SequenceProcessor>")]
    public struct SequenceNode : IBTNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        
        /// <summary> 当前节点索引值 </summary>
        private int m_CurrentIndex;

        
        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
        {
            while (m_CurrentIndex < Children.Length)
            {
                var status = Children[m_CurrentIndex].Run(ref bb, deltaTime);
        
                if (status == NodeStatus.Running) 
                    return NodeStatus.Running;
            
                if (status == NodeStatus.Failure)
                {
                    m_CurrentIndex = 0;
                    return NodeStatus.Failure;
                }
        
                m_CurrentIndex++;
            }
    
            m_CurrentIndex = 0;
            return NodeStatus.Success;
        }
        
        void IResetableNode.Reset()
        {
            m_CurrentIndex = 0;
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }
}