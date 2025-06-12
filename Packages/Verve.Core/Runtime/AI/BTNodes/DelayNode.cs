namespace Verve.AI
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct DelayNodeData : INodeData
    {
        [Serializable]
        public enum DelayResetMode : byte
        {
            /// <summary> 重新开始延时计时 </summary>
            Restart,
            /// <summary> 仅生效一次延时计时 </summary>
            Once
        }


        /// <summary> 等待时长（秒） </summary>
        public float Duration;
        /// <summary> 重置模式 </summary>
        public DelayResetMode Mode;
    }


    /// <summary>
    /// 延迟执行节点，在行为树中实现定时等待功能
    /// </summary>
    [Serializable]
    public struct DelayNode : IBTNode, IResetableNode
    {
        public DelayNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        /// <summary> 累计时间 </summary>
        private float m_ElapsedTime;
        /// <summary> 是否完成 </summary>
        private bool m_IsCompleted;
        
        public readonly float ElapsedTime => m_ElapsedTime;
        public readonly bool IsCompleted => m_IsCompleted;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            LastStatus = NodeStatus.Running;
            
            if (m_IsCompleted) 
                return NodeStatus.Success;
            if (Data.Duration <= 0.0f)
                return NodeStatus.Failure;
            
            m_ElapsedTime += ctx.DeltaTime;
            
            if (m_ElapsedTime < Data.Duration) return NodeStatus.Running;
            
            LastStatus = NodeStatus.Success;
            m_IsCompleted = true;
            return NodeStatus.Success;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            if (Data.Mode != DelayNodeData.DelayResetMode.Once)
            {
                m_ElapsedTime = 0f;
                m_IsCompleted = false;
            }
        }

        #endregion
    }
}