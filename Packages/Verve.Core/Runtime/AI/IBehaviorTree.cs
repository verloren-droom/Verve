namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 行为树接口
    /// </summary>
    public interface IBehaviorTree : IDisposable
    {
        int ID { get; }
        /// <summary>
        /// 是否激活
        /// </summary>
        bool IsActive { set; get; }
        /// <summary>
        /// 添加节点
        /// </summary>
        /// <param name="node"></param>
        /// <typeparam name="T"></typeparam>
        void AddNode<T>(in T node) where T : struct, IBTNode;
        Blackboard BB { get; set; }
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(in float deltaTime);
        void ResetNode(int nodeIndex);
        void ResetAllNodes(NodeResetMode resetMode = NodeResetMode.Full);
        NodeStatus GetNodeStatus(int nodeIndex);
        IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate);
        
        event Action<IBTNode, NodeStatus> OnNodeStatusChanged;
    }
}