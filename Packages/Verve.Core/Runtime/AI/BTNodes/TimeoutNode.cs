namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    
    
    /// <summary>
    /// 超时控制节点（在指定时间内完成子节点则成功，否则失败，未能完成将中断子节点的执行）
    /// </summary>
    [Serializable]
    public struct TimeoutNode : ICompositeNode, IResetableNode
    {
        /// <summary> 需要监控的子节点 </summary>
        [NotNull] public IBTNode Child;
        /// <summary> 最大等待时间（秒） </summary>
        public float Duration;

        /// <summary> 累计时间 </summary>
        private float m_ElapsedTime;
        /// <summary> 是否超时 </summary>
        private bool m_IsTimedOut;
        private NodeStatus m_LastChildStatus;
        
        public readonly float ElapsedTime => m_ElapsedTime;

        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (m_IsTimedOut || Duration <= 0f)
                return NodeStatus.Failure;

            m_LastChildStatus = Child.Run(ref ctx);
            m_ElapsedTime += ctx.DeltaTime;

            if (m_ElapsedTime >= Duration)
            {
                if (Child is IResetableNode resetable)
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

            return m_LastChildStatus;
        }

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_ElapsedTime = 0f;
            m_IsTimedOut = false;
        
            if (Child is IResetableNode resetable)
            {
                resetable.Reset(ref ctx);
            }
        }

        
        public int ChildCount => 1;
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => new[] { Child };
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            if (m_LastChildStatus == NodeStatus.Running)
            {
                yield return Child;
            }
        }
    }
}