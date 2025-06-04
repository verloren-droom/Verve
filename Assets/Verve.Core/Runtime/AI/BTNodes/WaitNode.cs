namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 等待时间节点
    /// </summary>
    [Serializable]
    public struct WaitNode : IBTNode, IResetableNode
    {
        [Serializable]
        public enum ResetMode : byte
        {
            /// <summary> 自动重置计时器 </summary>
            AutoReset,
            /// <summary> 仅生效一次 </summary>
            OneTime
        }

        
        /// <summary> 等待时长 </summary>
        public float Duration;
        /// <summary> 重置模式 </summary>
        public ResetMode ResetType;
        
        public float ElapsedTime => m_ElapsedTime;
        public bool IsCompleted => m_IsCompleted;
        
        /// <summary> 累计时间 </summary>
        private float m_ElapsedTime;
        /// <summary> 是否完成 </summary>
        private bool m_IsCompleted;


        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
        {
            if (Duration <= 0.0f)
                return NodeStatus.Failure;
            if (m_IsCompleted) 
                return NodeStatus.Success;
            
            m_ElapsedTime += deltaTime;
            if (m_ElapsedTime >= Duration)
            {
                m_IsCompleted = true;
                return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }
    
        void IResetableNode.Reset()
        {
            if (ResetType != ResetMode.OneTime)
            {
                m_ElapsedTime = 0f;
                m_IsCompleted = false;
            }
        }
    }


}