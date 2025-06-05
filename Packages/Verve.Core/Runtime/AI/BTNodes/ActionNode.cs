namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 动作节点（通过回调函数执行具体行为，回调返回值决定节点状态）
    /// </summary>
    [Serializable]
    public struct ActionNode : IBTNode
    {
        /// <summary> 动作回调 </summary>
        public Func<Blackboard, NodeStatus> Callback;
        
        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            return Callback?.Invoke(ctx.BB) ?? NodeStatus.Failure;
        }
    }
}