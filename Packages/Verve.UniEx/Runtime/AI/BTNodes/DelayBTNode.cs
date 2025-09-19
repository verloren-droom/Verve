namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 延时节点数据
    /// </summary>
    [Serializable]
    public struct DelayBTNodeData : INodeData
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
        public float duration;
        /// <summary> 重置模式 </summary>
        public DelayResetMode resetMode;
    }


    /// <summary>
    /// 延迟执行节点
    /// </summary>
    /// <remarks>
    /// 定时等待，直到指定时长结束，才返回成功
    /// </remarks>
    [CustomBTNode(nameof(DelayBTNode)), Serializable]
    public struct DelayBTNode : IBTNode, IBTNodeResettable
    {
        public DelayBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        /// <summary> 累计时间 </summary>
        private float m_ElapsedTime;
        /// <summary> 是否完成 </summary>
        private bool m_IsCompleted;
        
        public readonly float ElapsedTime => m_ElapsedTime;
        public readonly bool IsCompleted => m_IsCompleted;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            LastResult = BTNodeResult.Running;
            
            if (m_IsCompleted) 
                return BTNodeResult.Succeeded;
            if (data.duration <= 0.0f)
                return BTNodeResult.Failed;
            
            m_ElapsedTime += ctx.deltaTime;
            
            if (m_ElapsedTime < data.duration) return BTNodeResult.Running;
            
            LastResult = BTNodeResult.Succeeded;
            m_IsCompleted = true;
            return BTNodeResult.Succeeded;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            if (data.resetMode != DelayBTNodeData.DelayResetMode.Once)
            {
                m_ElapsedTime = 0f;
                m_IsCompleted = false;
            }
        }

        #endregion
    }
}