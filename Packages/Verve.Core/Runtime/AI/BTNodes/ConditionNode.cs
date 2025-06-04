namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 条件节点数据
    /// </summary>
    [Serializable]
    public struct ConditionData : INodeData
    {
        public Func<Blackboard, bool> Condition;
    }

    
    /// <summary>
    /// 条件节点处理器
    /// </summary>
    public struct ConditionProcessor : INodeProcessor<ConditionData>
    {
        public NodeStatus Run(ref ConditionData data, ref Blackboard bb, float deltaTime)
            => data.Condition?.Invoke(bb) == true ? NodeStatus.Success : NodeStatus.Failure;
    
        public void Reset(ref ConditionData data) {}
    }
    
    
    /// <summary>
    /// 条件节点（根据条件回调的返回值决定成功/失败状态）
    /// </summary>
    [Serializable]
    // [Obsolete("Please use BTNode<ConditionData, ConditionProcessor>")]
    public struct ConditionNode : IBTNode
    {
        /// <summary> 条件回调 </summary>
        public Func<Blackboard, bool> Condition;
        
        
        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
        {
            return Condition?.Invoke(bb) == true 
                ? NodeStatus.Success 
                : NodeStatus.Failure;
        }
    }
}