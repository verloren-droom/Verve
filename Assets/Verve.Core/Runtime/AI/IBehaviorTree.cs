namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 扩展行为树接口
    /// </summary>
    public interface IBehaviorTree : IDisposable
    {
        int ID { get; }
        void AddNode<T>(in T node) where T : struct, IAINode;
        Blackboard BB { get; set; }
        void Update(in float deltaTime);
        void ResetNode(int nodeIndex);
        void ResetAllNodes();
        NodeStatus GetNodeStatus(int nodeIndex);
        IEnumerable<IAINode> FindNodes(Func<IAINode, bool> predicate);
        
        event Action<IAINode, NodeStatus> OnNodeStatusChanged;
    }
}