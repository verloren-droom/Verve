using System.Collections.Generic;

namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 反转节点（将子节点结果取反）
    /// </summary>
    [Serializable]
    public struct InverterNode : ICompositeNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode Child;
        
        private NodeStatus m_LastChildStatus;
        
        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            m_LastChildStatus = Child.Run(ref ctx);
            return m_LastChildStatus switch {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => m_LastChildStatus
            };
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