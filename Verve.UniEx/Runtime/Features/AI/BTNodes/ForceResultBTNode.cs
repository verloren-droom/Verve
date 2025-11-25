namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    
    [Serializable]
    public enum ForceResultMode : byte
    {
        /// <summary>
        ///   <para>反转结果（成功变失败，失败变成功）</para>
        /// </summary>
        Invert,
        /// <summary>
        ///   <para>强制结果为成功</para>
        /// </summary>
        ForceSuccess,
        /// <summary>
        ///   <para>强制结果为失败</para>
        /// </summary>
        ForceFailure
    }

    
    /// <summary>
    ///   <para>强制结果节点数据</para>
    /// </summary>
    [Serializable]
    public struct ResultModifierBTNodeData : INodeData
    {
        /// <summary>
        ///   <para>子节点</para>
        /// </summary>
        [NotNull] public IBTNode child;
        /// <summary>
        ///   <para>强制结果模式</para>
        /// </summary>
        public ForceResultMode resultMode;
    }


    /// <summary>
    ///   <para>强制结果节点（修改子节点的执行结果）</para>
    /// </summary>
    [CustomBTNode(nameof(ForceResultBTNode)), Serializable]
    public struct ForceResultBTNode : ICompositeBTNode
    {
        /// <summary>
        ///   <para>节点数据</para>
        /// </summary>
        public ResultModifierBTNodeData data;
        public BTNodeResult LastResult { get; private set; }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            LastResult = this.RunChildNode(ref data.child, ref ctx);;
            
            return data.resultMode switch
            {
                ForceResultMode.Invert => 
                    LastResult == BTNodeResult.Succeeded ? BTNodeResult.Failed : 
                    LastResult == BTNodeResult.Failed ? BTNodeResult.Succeeded : 
                    LastResult,
                ForceResultMode.ForceSuccess => 
                    LastResult == BTNodeResult.Running ? BTNodeResult.Running : BTNodeResult.Succeeded,
                ForceResultMode.ForceFailure => 
                    LastResult == BTNodeResult.Running ? BTNodeResult.Running : BTNodeResult.Failed,
                _ => LastResult
            };
        }

        #region 复合节点

        public int ChildCount => 1;
        
        IEnumerable<IBTNode> ICompositeBTNode.GetChildren() => new[] { data.child };

        IEnumerable<IBTNode> ICompositeBTNode.GetActiveChildren()
        {
            if (LastResult == BTNodeResult.Running)
            {
                yield return data.child;
            }
        }

        #endregion
    }
}
