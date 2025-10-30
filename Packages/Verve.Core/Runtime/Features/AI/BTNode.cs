namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>行为树节点结果</para>
    /// </summary>
    [Serializable]
    public enum BTNodeResult : byte
    {
        /// <summary>
        ///   <para>节点运行中（阻塞当前节点直到完成）</para>
        /// </summary>
        [EnumMember(Value = "Running")]
        Running,
        
        /// <summary>
        ///   <para>节点成功（允许执行后续节点）</para>
        /// </summary>
        [EnumMember(Value = "Succeeded")]
        Succeeded,
        
        /// <summary>
        ///   <para>节点失败（中断当前行为层级，结果将传递给父节点处理）</para>
        /// </summary>
        [EnumMember(Value = "Failed")]
        Failed,
        
        // /// <summary>
        // ///   <para>节点被打断（由自身或更高优先级节点触发）</para>
        // /// </summary>
        // [EnumMember(Value = "Aborted")]
        // Aborted,
    }


    /// <summary>
    ///   <para>行为树基础节点接口（使用结构体实现接口）</para>
    ///   <para>所有行为树节点必须实现此接口，定义节点的执行逻辑</para>
    /// </summary>
    public interface IBTNode
    {
        /// <summary>
        ///   <para>获取节点结果</para>
        /// </summary>
        public BTNodeResult LastResult { get; }
        
        /// <summary>
        ///   <para>运行节点</para>
        /// </summary>
        /// <param name="ctx">节点运行上下文</param>
        /// <returns>
        ///   <para>节点结果</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult Run(ref BTNodeRunContext ctx);
    }


    /// <summary>
    ///   <para>行为树节点运行上下文</para>
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
    public struct BTNodeRunContext
    {
        /// <summary>
        ///   <para>关联黑板</para>
        /// </summary>
        [FieldOffset(0)]
        public IBlackboard bb;
        
        /// <summary>
        ///   <para>时间增量</para>
        /// </summary>
        [FieldOffset(8)]
        public float deltaTime;
        
        /// <summary>
        ///   <para>填充字段（占位预留）</para>
        /// </summary>
        [FieldOffset(12)]
        private int _padding;
    }

    
    /// <summary>
    ///   <para>行为树复合节点接口（使用结构体实现接口）</para>
    ///   <para>存在子节点的行为树节点必须实现此接口，控制子节点执行流程</para>
    /// </summary>
    public interface ICompositeBTNode : IBTNode
    {
        /// <summary>
        ///   <para>获取子节点总数</para>
        /// </summary>
        int ChildCount { get; }
        
        /// <summary>
        ///   <para>获取子节点</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerable<IBTNode> GetChildren();
        
        /// <summary>
        ///   <para>获取当前活跃的子节点</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerable<IBTNode> GetActiveChildren();
    }
    
    
    /// <summary>
    ///   <para>行为树节点能力增强接口</para>
    ///   <para>用于扩展节点功能，节点功能扩展应实现此接口</para>
    /// </summary>
    public interface IBTNodePlus { }

    
    /// <summary>
    ///   <para>可被重置的行为树节点</para>
    ///   <para>需要清理内部状态的节点应实现此接口，当节点被打断或行为树重置时会被调用</para>
    /// </summary>
    public interface IBTNodeResettable : IBTNodePlus
    {
        /// <summary>
        ///   <para>重置节点内部数据</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Reset(ref BTNodeResetContext ctx);
    }
    

    /// <summary>
    ///   <para>行为树节点重置模式</para>
    /// </summary>
    [Serializable]
    public enum BTNodeResetMode : byte
    {
        /// <summary>
        ///   <para>全部重置</para>
        /// </summary>
        [EnumMember(Value = "Full")]
        Full,
        
        /// <summary>
        ///   <para>部分重置</para>
        /// </summary>
        [EnumMember(Value = "Partial")]
        Partial,
        
        /// <summary>
        ///   <para>软重置</para>
        /// </summary>
        [EnumMember(Value = "Soft")]
        Soft,
    }
    

    /// <summary>
    ///   <para>行为树节点重置上下文</para>
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
    public struct BTNodeResetContext 
    {
        /// <summary>
        ///   <para>关联黑板</para>
        /// </summary>
        [FieldOffset(0)]
        public IBlackboard bb;
        
        /// <summary>
        ///   <para>重置模式</para>
        /// </summary>
        [FieldOffset(8)]
        public BTNodeResetMode resetMode;
        
        /// <summary>
        ///   <para>填充字段（占位预留）</para>
        /// </summary>
        [FieldOffset(12)]
        private int _padding;
    }
    
    
    /// <summary>
    ///   <para>可被准备的行为树节点</para>
    ///   <para>需要在运行前准备内部状态的节点应实现此接口，在节点执行前会被调用（如从黑板中实时获取最新值）</para>
    /// </summary>
    public interface IBTNodePreparable : IBTNodePlus
    {
        /// <summary>
        ///   <para>在运行前准备节点内部数据</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Prepare(ref BTNodeRunContext ctx);
    }


    /// <summary>
    ///   <para>节点数据接口（使用结构体实现接口）</para>
    /// </summary>
    public interface INodeData { }
}