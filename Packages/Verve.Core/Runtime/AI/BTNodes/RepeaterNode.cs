namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 重复执行节点
    /// </summary>
    [Serializable]
    public struct RepeaterNode : ICompositeNode, IResetableNode
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
        public IBTNode Child;
        /// <summary> 循环次数（仅限 RepeatMode.CountLimited 模式） </summary>
        public int RepeatCount;
        /// <summary> 重复模式 </summary>
        public RepeatMode Mode;
        
        private int m_CurrentChildIndex;
        private NodeStatus m_LastChildStatus;

        public int CurrentChildIndex => m_CurrentChildIndex;

        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (RepeatCount <= 0 && Mode == RepeatMode.CountLimited) return NodeStatus.Failure;
            if (CheckExitCondition()) return NodeStatus.Success;

            var status = Child.Run(ref ctx);
            m_LastChildStatus = status;

            if (status != NodeStatus.Running)
                m_CurrentChildIndex++;
            
            return NodeStatus.Running;
        }
        
        private bool CheckExitCondition()
        {
            return Mode switch
            {
                RepeatMode.CountLimited => m_CurrentChildIndex >= RepeatCount,
                RepeatMode.UntilSuccess => m_LastChildStatus == NodeStatus.Success,
                RepeatMode.UntilFailure => m_LastChildStatus == NodeStatus.Failure,
                _ => false
            };
        }

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            m_LastChildStatus = NodeStatus.Running;
            if (Child is IResetableNode resetable)
                resetable.Reset(ref ctx);
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