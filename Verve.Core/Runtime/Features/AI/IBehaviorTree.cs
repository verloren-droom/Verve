namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>行为树接口</para>
    /// </summary>
    public interface IBehaviorTree : IDisposable
    {
        /// <summary>
        ///   <para>树ID</para>
        /// </summary>
        string TreeID { get; }
        
        /// <summary>
        ///   <para>关联黑板</para>
        /// </summary>
        IBlackboard BB { get; set; }
        
        /// <summary>
        ///   <para>添加节点</para>
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="node">节点</param>
        void AddNode<T>(in T node) where T : struct, IBTNode;
        
        /// <summary>
        ///   <para>更新节点</para>
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(in float deltaTime);
        
        /// <summary>
        ///   <para>重置节点</para>
        /// </summary>
        /// <param name="nodeIndex">节点索引</param>
        void ResetNode(int nodeIndex);
        
        /// <summary>
        ///   <para>重置所有节点</para>
        /// </summary>
        /// <param name="resetMode">重置模式</param>
        void ResetAllNodes(BTNodeResetMode resetMode = BTNodeResetMode.Full);
        
        /// <summary>
        ///   <para>查找节点</para>
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns>
        ///   <para>符合条件的节点</para>
        /// </returns>
        IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate);
        
        /// <summary>
        ///   <para>暂停行为树执行</para>
        /// </summary>
        void Pause();
        
        /// <summary>
        ///   <para>恢复行为树执行</para>
        /// </summary>
        void Resume();
        
        /// <summary>
        ///   <para>获取当前执行路径</para>
        /// </summary>
        /// <returns>
        ///   <para>当前执行路径</para>
        /// </returns>
        IEnumerable<string> GetActivePath();
        
        /// <summary>
        ///   <para>节点结果改变事件</para>
        /// </summary>
        event Action<IBTNode, BTNodeResult> OnNodeResultChanged;
    }
}