#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using Verve.AI;
    using UnityEngine;
    using System.Diagnostics.CodeAnalysis;
    
    
    /// <summary>
    /// 节点可被调试
    /// </summary>
    public interface IDebuggableNode
    {
        /// <summary> 节点调试目标 </summary>
        [NotNull] GameObject DebugTarget { get; }
        /// <summary> 是否被调试 </summary>
        bool IsDebug { get; set; }
        /// <summary> 节点名称 </summary>
        string NodeName { get; }
        /// <summary> 绘制节点 Gizmos </summary>
        void DrawGizmos(ref NodeDebugContext ctx);
    }

    
    /// <summary>
    /// 节点调试上下文
    /// </summary>
    public struct NodeDebugContext
    {
        /// <summary> 关联黑板 </summary>
        public Blackboard BB;
    }
}

#endif