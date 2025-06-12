namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 行为树接口
    /// </summary>
    public interface IBehaviorTree : IDisposable
    {
        /// <summary> 树ID </summary>
        int ID { get; }
        /// <summary> 关联黑板 </summary>
        Blackboard BB { get; set; }
        /// <summary>
        /// 添加节点
        /// </summary>
        /// <param name="node"></param>
        /// <typeparam name="T"></typeparam>
        void AddNode<T>(in T node) where T : struct, IBTNode;
        /// <summary>
        /// 更新节点
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(in float deltaTime);
        void ResetNode(int nodeIndex);
        void ResetAllNodes(NodeResetMode resetMode = NodeResetMode.Full);
        /// <summary>
        /// 查找节点
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate);
        /// <summary> 暂停行为树执行 </summary>
        void Pause();
        /// <summary> 恢复行为树执行 </summary>
        void Resume();
        IEnumerable<string> GetActivePath();
        event Action<IBTNode, NodeStatus> OnNodeStatusChanged;
    }
}