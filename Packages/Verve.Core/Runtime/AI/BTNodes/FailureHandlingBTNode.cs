namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    

    /// <summary>
    /// 失败处理模式
    /// </summary>
    [Serializable]
    public enum FailureHandlingMode : byte
    {
        /// <summary> 跳过失败并返回成功 </summary>
        Skip,
        /// <summary> 捕获失败并执行备用逻辑 </summary>
        Catch,
        /// <summary> 抛出失败并中断执行 </summary>
        Throw
    }
    
    
    /// <summary>
    /// 失败处理节点数据
    /// </summary>
    [Serializable]
    public struct FailureHandlingBTNodeData : INodeData
    {
        /// <summary> 需要监控的子节点 </summary>
        [NotNull] public IBTNode child;
        /// <summary> 失败处理模式 </summary>
        public FailureHandlingMode handlingMode;
        /// <summary> 备用执行路径（仅在Catch模式下有效）</summary>
        [NotNull] public IBTNode fallback;
    }
    
    
    /// <summary>
    /// 失败处理节点
    /// </summary>
    /// <remarks>
    /// 节点运行时，会尝试运行子节点，如果子节点运行失败，则根据模式处理错误
    /// </remarks>
    [CustomBTNode(nameof(FailureHandlingBTNode)), Serializable]
    public struct FailureHandlingBTNode : ICompositeBTNode, IBTNodeResettable, IBTNodePreparable
    {
        /// <summary> 黑板数据键 </summary>
        public string dataKey;
        public FailureHandlingBTNodeData data;
    
        public BTNodeResult LastResult { get; private set; }
    
        private bool m_HasHandledFail;
        private BTNodeResult m_FallbackResult;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            var childResult = data.child.Run(ref ctx);
    
            if (childResult != BTNodeResult.Failed)
            {
                LastResult = childResult;
                return childResult;
            }
    
            if (!m_HasHandledFail)
            {
                switch (data.handlingMode)
                {
                    case FailureHandlingMode.Skip:
                        LastResult = BTNodeResult.Succeeded;
                        m_HasHandledFail = true;
                        break;
                    case FailureHandlingMode.Catch:
                        m_FallbackResult = data.fallback.Run(ref ctx);
                        LastResult = m_FallbackResult;
                        m_HasHandledFail = true;
                        break;
                    case FailureHandlingMode.Throw:
                        LastResult = BTNodeResult.Failed;
                        m_HasHandledFail = true;
                        return BTNodeResult.Failed;
                    default:
                        LastResult = BTNodeResult.Failed;
                        m_HasHandledFail = true;
                        break;
                }
            }
    
            if (data.handlingMode == FailureHandlingMode.Catch && m_FallbackResult == BTNodeResult.Running)
            {
                return BTNodeResult.Running;
            }
    
            return LastResult;
        }
    
        #region 复合节点接口
    
        public int ChildCount => data.handlingMode == FailureHandlingMode.Catch ? 2 : 1;
    
        IEnumerable<IBTNode> ICompositeBTNode.GetChildren()
        {
            if (data.handlingMode == FailureHandlingMode.Catch)
            {
                return new[] { data.child, data.fallback };
            }
            return new[] { data.child };
        }
    
        IEnumerable<IBTNode> ICompositeBTNode.GetActiveChildren()
        {
            if (LastResult == BTNodeResult.Running)
            {
                yield return data.child;
                if (m_HasHandledFail && data.handlingMode == FailureHandlingMode.Catch)
                    yield return data.fallback;
            }
        }
    
        #endregion

        #region 可重置节点
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_HasHandledFail = false;
            m_FallbackResult = BTNodeResult.Running;
            this.ResetChildrenNode(ref ctx);
        }
    
        #endregion
    
        #region 可准备节点
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodePreparable.Prepare(ref BTNodeRunContext ctx)
        {
            if (ctx.bb.HasValue(dataKey))
            {
                data = ctx.bb.GetValue<FailureHandlingBTNodeData>(dataKey);
            }
        }
    
        #endregion
    }

}