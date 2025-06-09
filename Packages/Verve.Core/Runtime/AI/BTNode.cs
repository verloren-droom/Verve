namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    
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
        /// <param name="ctx"> 节点运行上下文 </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus Run(ref NodeRunContext ctx);
    }
    
    
    /// <summary>
    /// 行为树复合节点接口（使用结构体实现接口）
    /// </summary>
    /// <remarks>
    /// 存在子节点的行为树节点必须实现此接口
    /// </remarks>
    public interface ICompositeNode : IBTNode
    {
        /// <summary>
        /// 获取子节点总数
        /// </summary>
        int ChildCount { get; }
        /// <summary>
        /// 获取子节点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerable<IBTNode> GetChildren();
        /// <summary>
        /// 获取当前活跃的子节点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerable<IBTNode> GetActiveChildren();
    }

    
    /// <summary>
    /// 节点运行上下文
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
    public struct NodeRunContext
    {
        /// <summary> 关联黑板 </summary>
        [FieldOffset(0)]
        public Blackboard BB;
        /// <summary> 时间增量 </summary>
        [FieldOffset(8)]
        public float DeltaTime;
        
        [FieldOffset(12)]
        private int _padding;
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
        /// <summary> 重置节点内部数据 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Reset(ref  NodeResetContext ctx);
    }

    
    /// <summary>
    /// 节点重置模式
    /// </summary>
    [Serializable]
    public enum NodeResetMode : byte
    {
        /// <summary> 全部重置 </summary>
        Full,
        /// <summary> 部分重置 </summary>
        Partial,
        /// <summary> 软重置 </summary>
        Soft,
    }
    
    
    /// <summary>
    /// 节点重置上下文
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
    public struct NodeResetContext 
    {
        /// <summary> 关联黑板 </summary>
        [FieldOffset(0)]
        public Blackboard BB;
        /// <summary> 重置模式 </summary>
        [FieldOffset(8)]
        public NodeResetMode Mode;
        
        [FieldOffset(12)]
        private int _padding;
    }


    /// <summary>
    /// 节点数据接口（使用结构体实现接口）
    /// </summary>
    public interface INodeData { }
    
    
    /// <summary>
    /// 节点逻辑处理器接口（使用结构体实现接口）
    /// </summary>
    /// <typeparam name="TData"> 节点数据类型 </typeparam>
    public interface INodeProcessor<TData>
        where TData : struct, INodeData
    {
        NodeStatus Run(ref TData data, ref NodeRunContext ctx);
        void Reset(ref TData data, ref NodeResetContext ctx);
    }
    
    
    /// <summary>
    /// 通用行为树节点结构体
    /// </summary>
    /// <remarks> 使用通用行为树节点可使数据（INodeData）和逻辑（INodeProcessor）解藕 </remarks>
    /// <typeparam name="TData"> 节点数据类型 </typeparam>
    /// <typeparam name="TProcessor"> 节点逻辑处理器类型 </typeparam>
    [Serializable]
    public struct BTNode<TData, TProcessor> : IBTNode, IResetableNode
        where TData : struct, INodeData
        where TProcessor : struct, INodeProcessor<TData>
    {
        public TData Data;
        private TProcessor m_Processor;
        
        
        public NodeStatus Run(ref NodeRunContext ctx) => m_Processor.Run(ref Data, ref ctx);
    
        public void Reset(ref NodeResetContext ctx) => m_Processor.Reset(ref Data, ref ctx);
    }
}