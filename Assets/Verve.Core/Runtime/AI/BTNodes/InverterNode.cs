namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 反转节点（将子节点结果取反）
    /// </summary>
    [Serializable]
    public struct InverterNode : IBTNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode Child;
        
        
        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
        {
            var status = Child.Run(ref bb, deltaTime);
            return status switch {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => status
            };
        }
    }
}