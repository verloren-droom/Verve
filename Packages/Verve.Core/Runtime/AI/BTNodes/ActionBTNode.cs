namespace Verve.AI
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 动作节点数据
    /// </summary>
    [Serializable]
    public struct ActionBTNodeData : INodeData
    {
        /// <summary> 动作回调 </summary>
        [NonSerialized]
        public Func<Blackboard, BTNodeResult> callback;
    }


    /// <summary>
    /// 动作节点
    /// </summary>
    /// <remarks>
    /// 通过回调函数执行具体行为，回调返回值决定节点结果
    /// </remarks>
    [CustomBTNode(nameof(ActionBTNode)), Serializable]
    public struct ActionBTNode : IBTNode
    {
        public ActionBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            LastResult = data.callback?.Invoke(ctx.bb) ?? BTNodeResult.Failed;
            return LastResult;
        }
    }
}