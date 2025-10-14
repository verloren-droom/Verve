namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 行为树节点结果
    /// </summary>
    [Serializable]
    public enum BTNodeResult : byte
    {
        /// <summary> 节点运行中（阻塞当前节点直到完成） </summary>
        [EnumMember(Value = "Running")]
        Running,
        /// <summary> 节点成功（允许执行后续节点） </summary>
        [EnumMember(Value = "Succeeded")]
        Succeeded,
        /// <summary> 节点失败（中断当前行为层级，结果将传递给父节点处理） </summary>
        [EnumMember(Value = "Failed")]
        Failed,
        // /// <summary> 节点被打断（由自身或更高优先级节点触发） </summary>
        // [EnumMember(Value = "Aborted")]
        // Aborted,
    }


    /// <summary>
    /// 行为树基础节点接口（使用结构体实现接口）
    /// </summary>
    /// <remarks>
    /// 所有行为树节点必须实现此接口，定义节点的执行逻辑
    /// </remarks>
    public interface IBTNode
    {
        /// <summary> 获取节点结果 </summary>
        public BTNodeResult LastResult { get; }
        /// <summary>
        /// 运行节点
        /// </summary>
        /// <param name="ctx"> 节点运行上下文 </param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult Run(ref BTNodeRunContext ctx);
    }


    /// <summary>
    /// 行为树节点运行上下文
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
    public struct BTNodeRunContext
    {
        /// <summary> 关联黑板 </summary>
        [FieldOffset(0)]
        public IBlackboard bb;
        /// <summary> 时间增量 </summary>
        [FieldOffset(8)]
        public float deltaTime;
        
        /// <summary> 填充字段（占位预留） </summary>
        [FieldOffset(12)]
        private int _padding;
    }

    
    /// <summary>
    /// 行为树复合节点接口（使用结构体实现接口）
    /// </summary>
    /// <remarks>
    /// 存在子节点的行为树节点必须实现此接口，控制子节点执行流程
    /// </remarks>
    public interface ICompositeBTNode : IBTNode
    {
        /// <summary> 获取子节点总数 </summary>
        int ChildCount { get; }
        /// <summary> 获取子节点 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerable<IBTNode> GetChildren();
        /// <summary> 获取当前活跃的子节点 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerable<IBTNode> GetActiveChildren();
    }
    
    
    /// <summary>
    /// 行为树节点能力增强接口
    /// </summary>
    /// <remarks>
    /// 用于扩展节点功能，节点功能扩展应实现此接口
    /// </remarks>
    public interface IBTNodePlus { }

    
    /// <summary>
    /// 可被重置的行为树节点
    /// </summary>
    /// <remarks>
    /// 需要清理内部状态的节点应实现此接口，当节点被打断或行为树重置时会被调用
    /// </remarks>
    public interface IBTNodeResettable : IBTNodePlus
    {
        /// <summary> 重置节点内部数据 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Reset(ref BTNodeResetContext ctx);
    }
    

    /// <summary>
    /// 行为树节点重置模式
    /// </summary>
    [Serializable]
    public enum BTNodeResetMode : byte
    {
        /// <summary> 全部重置 </summary>
        [EnumMember(Value = "Full")]
        Full,
        /// <summary> 部分重置 </summary>
        [EnumMember(Value = "Partial")]
        Partial,
        /// <summary> 软重置 </summary>
        [EnumMember(Value = "Soft")]
        Soft,
    }
    

    /// <summary>
    /// 行为树节点重置上下文
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 12)]
    public struct BTNodeResetContext 
    {
        /// <summary> 关联黑板 </summary>
        [FieldOffset(0)]
        public IBlackboard bb;
        /// <summary> 重置模式 </summary>
        [FieldOffset(8)]
        public BTNodeResetMode resetMode;
        
        /// <summary> 填充字段（占位预留） </summary>
        [FieldOffset(12)]
        private int _padding;
    }
    
    
    /// <summary>
    /// 可被准备的行为树节点
    /// </summary>
    /// <remarks>
    /// 需要在运行前准备内部状态的节点应实现此接口，在节点执行前会被调用（如从黑板中实时获取最新值）
    /// </remarks>
    public interface IBTNodePreparable : IBTNodePlus
    {
        /// <summary> 在运行前准备节点内部数据 </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Prepare(ref BTNodeRunContext ctx);
    }


    /// <summary>
    /// 节点数据接口（使用结构体实现接口）
    /// </summary>
    public interface INodeData { }
}