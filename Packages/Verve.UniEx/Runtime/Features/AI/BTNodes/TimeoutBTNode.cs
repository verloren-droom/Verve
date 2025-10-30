namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>超时控制节点数据</para>
    /// </summary>
    [Serializable]
    public struct TimeoutBTNodeData : INodeData
    {
        /// <summary>
        ///   <para>需要监控的子节点</para>
        /// </summary>
        [NotNull] public IBTNode child;
        /// <summary>
        ///   <para>最大等待时间（秒）</para>
        /// </summary>
        public float duration;
    }
    
    
    /// <summary>
    ///   <para>超时控制节点</para>
    ///   <para>在指定时间内完成子节点则成功，否则失败，未能完成将中断子节点的执行</para>
    /// </summary>
    [CustomBTNode(nameof(TimeoutBTNode)), Serializable]
    public struct TimeoutBTNode : ICompositeBTNode, IBTNodeResettable, IBTNodePreparable
    {
        /// <summary>
        ///   <para>黑板数据键</para>
        /// </summary>
        public string dataKey;
        public TimeoutBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private float m_ElapsedTime;
        private bool m_IsTimedOut;
        
        /// <summary>
        ///   <para>累计时间</para>
        /// </summary>
        public readonly float ElapsedTime => m_ElapsedTime;
        
        /// <summary>
        ///   <para>是否超时</para>
        /// </summary>
        public readonly bool IsTimedOut => m_IsTimedOut;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (m_IsTimedOut || data.duration <= 0f)
                return BTNodeResult.Failed;
            
            LastResult = this.RunChildNode(ref data.child, ref ctx);
            m_ElapsedTime += ctx.deltaTime;

            if (m_ElapsedTime >= data.duration)
            {
                if (data.child is IBTNodeResettable resetable)
                {
                    var resetCtx = new BTNodeResetContext {
                        bb = ctx.bb,
                        resetMode = BTNodeResetMode.Partial
                    };
                    resetable.Reset(ref resetCtx);
                }
            
                m_IsTimedOut = true;
                return BTNodeResult.Failed;
            }

            return LastResult;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_ElapsedTime = 0f;
            m_IsTimedOut = false;
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
                data = ctx.bb.GetValue<TimeoutBTNodeData>(dataKey);
            }
        }

        #endregion
    }
}