#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>动作节点数据</para>
    /// </summary>
    [Serializable]
    public struct ActionBTNodeData : INodeData
    {
        /// <summary>
        ///   <para>动作回调</para>
        /// </summary>
        [NonSerialized]
        public Func<IBlackboard, BTNodeResult> callback;
    }


    /// <summary>
    ///   <para>动作节点</para>
    ///   <para>通过回调函数执行具体行为，回调返回值决定节点结果</para>
    /// </summary>
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

#endif