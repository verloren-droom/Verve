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
        void AddNode<T>(in T node) where T : struct, IBTNode;
        Blackboard BB { get; set; }
        void Update(in float deltaTime);
        void ResetNode(int nodeIndex);
        void ResetAllNodes();
        NodeStatus GetNodeStatus(int nodeIndex);
        IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate);
        
        event Action<IBTNode, NodeStatus> OnNodeStatusChanged;
    }
}