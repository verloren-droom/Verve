namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 超时控制节点（在指定时间内完成子节点则成功，否则失败）
    /// </summary>
    [Serializable]
    public struct TimeoutNode : IBTNode, IResetableNode
    {
        /// <summary> 最大等待时间（秒） </summary>
        public float TimeoutDuration;
        /// <summary> 需要监控的子节点 </summary>
        public IBTNode Child;

        private float m_ElapsedTime;
        private bool m_IsRunning;
        
        public float ElapsedTime => m_ElapsedTime;


        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            m_ElapsedTime += ctx.DeltaTime;
            
            if (m_ElapsedTime > TimeoutDuration)
                return NodeStatus.Failure;

            var status = Child.Run(ref ctx);
            m_IsRunning = status == NodeStatus.Running;
            
            return status == NodeStatus.Running 
                ? NodeStatus.Running 
                : status;
        }

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_ElapsedTime = 0f;
            m_IsRunning = false;
        }
    }
}