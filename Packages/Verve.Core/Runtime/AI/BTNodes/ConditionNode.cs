namespace Verve.AI
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct ConditionNodeData : INodeData
    {
        /// <summary> 条件回调 </summary>
        public Func<Blackboard, bool> Condition;
    }
    
    
    /// <summary>
    /// 条件节点（根据条件回调的返回值决定成功/失败状态）
    /// </summary>
    [Serializable]
    public struct ConditionNode : IBTNode
    {
        public ConditionNodeData Data;
        public NodeStatus LastStatus { get; private set; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            LastStatus = Data.Condition?.Invoke(ctx.BB) == true 
                ? NodeStatus.Success 
                : NodeStatus.Failure;
            return LastStatus;
        }
    }
}