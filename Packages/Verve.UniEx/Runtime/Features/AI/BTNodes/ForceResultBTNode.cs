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
        /// <summary> 反转结果（成功变失败，失败变成功） </summary>
        Invert,
        /// <summary> 强制结果为成功 </summary>
        ForceSuccess,
        /// <summary> 强制结果为失败 </summary>
        ForceFailure
    }

    
    /// <summary>
    /// 强制结果节点数据
    /// </summary>
    [Serializable]
    public struct ResultModifierBTNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        [NotNull] public IBTNode child;
        /// <summary> 强制结果模式 </summary>
        public ForceResultMode resultMode;
    }


    /// <summary>
    /// 强制结果节点（修改子节点的执行结果）
    /// </summary>
    /// <remarks>
    /// 强制修改子节点的执行结果
    /// </remarks>
    [CustomBTNode(nameof(ForceResultBTNode)), Serializable]
    public struct ForceResultBTNode : ICompositeBTNode
    {
        /// <summary> 节点数据 </summary>
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
