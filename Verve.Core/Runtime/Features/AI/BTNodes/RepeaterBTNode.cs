namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>重复执行节点数据</para>
    /// </summary>
    [Serializable]
    public struct RepeaterBTNodeData : INodeData
    {
        [Serializable]
        public enum RepeatMode : byte
        {
            /// <summary>
            ///   <para>无限</para>
            /// </summary>
            Infinite,
            /// <summary>
            ///   <para>次数限制</para>
            /// </summary>
            CountLimited,
            /// <summary>
            ///   <para>直到成功</para>
            /// </summary>
            UntilSuccess,
            /// <summary>
            ///   <para>直到失败</para>
            /// </summary>
            UntilFailure
        }
        
        
        /// <summary>
        ///   <para>子节点</para>
        /// </summary>
        public IBTNode child;
        /// <summary>
        ///   <para>循环次数（仅限 RepeatMode.CountLimited 模式）</para>
        /// </summary>
        public int repeatCount;
        /// <summary>
        ///   <para>重复模式</para>
        /// </summary>
        public RepeatMode repeatMode;
    }
    
    
    /// <summary>
    ///   <para>重复执行节点</para>
    ///   <para>循环执行子节点，直到子节点返回成功或失败</para>
    /// </summary>
    [CustomBTNode(nameof(RepeaterBTNode)), Serializable]
    public struct RepeaterBTNode : ICompositeBTNode, IBTNodeResettable, IBTNodePreparable
    {
        /// <summary>
        ///   <para>黑板数据键</para>
        /// </summary>
        public string dataKey;
        public RepeaterBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private int m_CurrentChildIndex;

        public readonly int CurrentChildIndex => m_CurrentChildIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (data.repeatCount <= 0 && data.repeatMode == RepeaterBTNodeData.RepeatMode.CountLimited) return BTNodeResult.Failed;
            if (CheckExitCondition()) return BTNodeResult.Succeeded;

            LastResult = this.RunChildNode(ref data.child, ref ctx);

            if (LastResult != BTNodeResult.Running)
                m_CurrentChildIndex++;
            
            return BTNodeResult.Running;
        }
        
        private bool CheckExitCondition()
        {
            return data.repeatMode switch
            {
                RepeaterBTNodeData.RepeatMode.CountLimited => m_CurrentChildIndex >= data.repeatCount,
                RepeaterBTNodeData.RepeatMode.UntilSuccess => LastResult == BTNodeResult.Succeeded,
                RepeaterBTNodeData.RepeatMode.UntilFailure => LastResult == BTNodeResult.Failed,
                _ => false
            };
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            LastResult = BTNodeResult.Running;
            this.ResetChildrenNode(ref ctx);
        }

        #endregion

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
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodePreparable.Prepare(ref BTNodeRunContext ctx)
        {
            if (ctx.bb.HasValue(dataKey))
            {
                data = ctx.bb.GetValue<RepeaterBTNodeData>(dataKey);
            }
        }
        
        #endregion
    }
}