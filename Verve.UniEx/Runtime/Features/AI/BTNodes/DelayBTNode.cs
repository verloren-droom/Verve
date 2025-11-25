namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>延时节点数据</para>
    /// </summary>
    [Serializable]
    public struct DelayBTNodeData : INodeData
    {
        [Serializable]
        public enum DelayResetMode : byte
        {
            /// <summary>
            ///   <para>重新开始延时计时</para>
            /// </summary>
            Restart,
            /// <summary>
            ///   <para>仅生效一次延时计时</para>
            /// </summary>
            Once
        }


        /// <summary>
        ///   <para>等待时长（秒）</para>
        /// </summary>
        public float duration;
        /// <summary>
        ///   <para>重置模式</para>
        /// </summary>
        public DelayResetMode resetMode;
    }


    /// <summary>
    ///   <para>延迟执行节点</para>
    ///   <para>定时等待，直到指定时长结束，才返回成功</para>
    /// </summary>
    [CustomBTNode(nameof(DelayBTNode)), Serializable]
    public struct DelayBTNode : IBTNode, IBTNodeResettable
    {
        public DelayBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private float m_ElapsedTime;
        private bool m_IsCompleted;
        
        /// <summary>
        ///   <para>累计时间</para>
        /// </summary>
        public readonly float ElapsedTime => m_ElapsedTime;
        /// <summary>
        ///   <para>是否完成</para>
        /// </summary>
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