namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    [Serializable]
    public struct TimeoutNodeData : INodeData
    {
        /// <summary> 需要监控的子节点 </summary>
        [NotNull] public IBTNode Child;
        /// <summary> 最大等待时间（秒） </summary>
        public float Duration;
    }
    
    
    /// <summary>
    /// 超时控制节点（在指定时间内完成子节点则成功，否则失败，未能完成将中断子节点的执行）
    /// </summary>
    [Serializable]
    public struct TimeoutNode : ICompositeNode, IResetableNode, IPreparableNode
    {
        /// <summary>
        /// 黑板键，用于存储节点数据
        /// </summary>
        public string Key;
        public TimeoutNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        /// <summary> 累计时间 </summary>
        private float m_ElapsedTime;
        /// <summary> 是否超时 </summary>
        private bool m_IsTimedOut;
        
        public readonly float ElapsedTime => m_ElapsedTime;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (m_IsTimedOut || Data.Duration <= 0f)
                return NodeStatus.Failure;
            
            LastStatus = this.RunChildNode(ref Data.Child, ref ctx);
            m_ElapsedTime += ctx.DeltaTime;

            if (m_ElapsedTime >= Data.Duration)
            {
                if (Data.Child is IResetableNode resetable)
                {
                    var resetCtx = new NodeResetContext {
                        BB = ctx.BB,
                        Mode = NodeResetMode.Partial
                    };
                    resetable.Reset(ref resetCtx);
                }
            
                m_IsTimedOut = true;
                return NodeStatus.Failure;
            }

            return LastStatus;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_ElapsedTime = 0f;
            m_IsTimedOut = false;
        
            if (Data.Child is IResetableNode resetable)
            {
                resetable.Reset(ref ctx);
            }
        }

        #endregion

        #region 复合节点

        public int ChildCount => 1;
        
        
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => new[] { Data.Child };
        
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            if (LastStatus == NodeStatus.Running)
            {
                yield return Data.Child;
            }
        }

        #endregion
        
        #region 可准备节点
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<TimeoutNodeData>(Key);
            }
        }

        #endregion
    }
}