namespace Verve.AI
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct ActionNodeData : INodeData
    {
        /// <summary> 动作回调 </summary>
        public Func<Blackboard, NodeStatus> Callback;
    }


    /// <summary>
    /// 动作节点（通过回调函数执行具体行为，回调返回值决定节点状态）
    /// </summary>
    [Serializable]
    public struct ActionNode : IBTNode
    {
        public ActionNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            LastStatus = Data.Callback?.Invoke(ctx.BB) ?? NodeStatus.Failure;
            return LastStatus;
        }
    }
}