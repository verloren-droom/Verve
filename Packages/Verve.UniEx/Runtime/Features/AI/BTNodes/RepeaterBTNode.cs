namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 重复执行节点数据
    /// </summary>
    [Serializable]
    public struct RepeaterBTNodeData : INodeData
    {
        [Serializable]
        public enum RepeatMode : byte
        {
            /// <summary> 无限 </summary>
            Infinite,
            /// <summary> 次数限制 </summary>
            CountLimited,
            /// <summary> 直到成功 </summary>
            UntilSuccess,
            /// <summary> 直到失败 </summary>
            UntilFailure
        }
        
        
        /// <summary> 子节点 </summary>
        public IBTNode child;
        /// <summary> 循环次数（仅限 RepeatMode.CountLimited 模式） </summary>
        public int repeatCount;
        /// <summary> 重复模式 </summary>
        public RepeatMode repeatMode;
    }
    
    
    /// <summary>
    /// 重复执行节点
    /// </summary>
    /// <remarks>
    /// 循环执行子节点，直到子节点返回成功或失败
    /// </remarks>
    [CustomBTNode(nameof(RepeaterBTNode)), Serializable]
    public struct RepeaterBTNode : ICompositeBTNode, IBTNodeResettable, IBTNodePreparable
    {
        /// <summary> 黑板数据键 </summary>
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