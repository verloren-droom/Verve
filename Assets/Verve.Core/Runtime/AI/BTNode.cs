namespace Verve.AI
{
    using System;

    
    /// <summary>
    /// 节点状态枚举
    /// </summary>
    [Serializable]
    public enum NodeStatus : byte
    {
        /// <summary> 节点运行中（阻塞当前节点直到完成） </summary>
        Running,
        /// <summary> 节点成功（允许执行后续节点） </summary>
        Success,
        /// <summary> 节点失败（中断当前行为层级，结果将传递给父节点处理） </summary>
        Failure
    }


    /// <summary>
    /// 行为树节点接口（使用结构体实现接口）
    /// </summary>
    /// <remarks>
    /// 所有行为树节点必须实现此接口，
    /// 定义节点的执行逻辑
    /// </remarks>
    public interface IBTNode
    {
        /// <summary>
        /// 运行节点
        /// </summary>
        /// <param name="bb">共享数据黑板</param>
        /// <param name="deltaTime">时间增量</param>
        /// <returns></returns>
        NodeStatus Run(ref Blackboard bb, float deltaTime);
    }
    
    
    /// <summary>
    /// 可重置节点接口（使用结构体实现接口）
    /// </summary>
    /// <remarks>
    /// 需要清理内部状态的节点应实现此接口，
    /// 当节点被打断或行为树重置时会被调用
    /// </remarks>
    public interface IResetableNode
    {
        /// <summary>
        /// 重置节点内部状态
        /// </summary>
        void Reset();
    }
    
    
    /// <summary>
    /// 节点数据接口（使用结构体实现接口）
    /// </summary>
    public interface INodeData { }
    
    
    /// <summary>
    /// 节点逻辑处理器接口（使用结构体实现接口）
    /// </summary>
    /// <typeparam name="TData"> 节点数据类型 </typeparam>
    public interface INodeProcessor<TData> where TData : struct, INodeData
    {
        NodeStatus Run(ref TData data, ref Blackboard bb, float deltaTime);
        void Reset(ref TData data);
    }
    
    
    /// <summary>
    /// 通用行为树节点结构体
    /// </summary>
    /// <remarks> 使用通用行为树节点可使数据和逻辑解藕 </remarks>
    /// <typeparam name="TData"> 节点数据类型 </typeparam>
    /// <typeparam name="TProcessor"> 节点逻辑处理器类型 </typeparam>
    [Serializable]
    public struct BTNode<TData, TProcessor> : IBTNode, IResetableNode
        where TData : struct, INodeData
        where TProcessor : struct, INodeProcessor<TData>
    {
        public TData Data;
        private TProcessor m_Processor;
        
        
        public NodeStatus Run(ref Blackboard bb, float deltaTime) 
            => m_Processor.Run(ref Data, ref bb, deltaTime);
    
        public void Reset() => m_Processor.Reset(ref Data);
    }
}