namespace Verve.AI
{
    using System;

    
    /// <summary>
    /// 条件节点（根据条件回调的返回值决定成功/失败状态）
    /// </summary>
    [Serializable]
    public struct ConditionNode : IBTNode
    {
        /// <summary> 条件回调 </summary>
        public Func<Blackboard, bool> Condition;
        
        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            return Condition?.Invoke(ctx.BB) == true 
                ? NodeStatus.Success 
                : NodeStatus.Failure;
        }
    }
}