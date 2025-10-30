namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>条件节点数据</para>
    /// </summary>
    [Serializable]
    public struct ConditionBTNodeData : INodeData
    {
        /// <summary>
        ///   <para>条件回调</para>
        /// </summary>
        [NonSerialized]
        public Func<IBlackboard, bool> condition;
    }
    
    
    /// <summary>
    ///   <para>条件节点</para>
    ///   <para>根据条件回调的返回值决定成功/失败结果</para>
    /// </summary>
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