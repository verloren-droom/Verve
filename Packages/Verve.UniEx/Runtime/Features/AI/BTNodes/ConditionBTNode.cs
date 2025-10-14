namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 条件节点数据
    /// </summary>
    [Serializable]
    public struct ConditionBTNodeData : INodeData
    {
        /// <summary> 条件回调 </summary>
        [NonSerialized]
        public Func<IBlackboard, bool> condition;
    }
    
    
    /// <summary>
    /// 条件节点
    /// </summary>
    /// <remarks>
    /// 根据条件回调的返回值决定成功/失败结果
    /// </remarks>
    [CustomBTNode(nameof(ConditionBTNode)), Serializable]
    public struct ConditionBTNode : IBTNode
    {
        public ConditionBTNodeData data;
        public BTNodeResult LastResult { get; private set; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            LastResult = data.condition?.Invoke(ctx.bb) == true 
                ? BTNodeResult.Succeeded 
                : BTNodeResult.Failed;
            return LastResult;
        }
    }
}