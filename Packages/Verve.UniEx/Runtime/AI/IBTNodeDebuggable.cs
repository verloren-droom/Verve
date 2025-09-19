#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    using Verve.AI;


    /// <summary>
    /// 可被调试的行为树节点
    /// </summary>
    public interface IBTNodeDebuggable
    {
        /// <summary> 是否被调试 </summary>
        bool IsDebug { get; set; }
    }

    
    /// <summary>
    /// 节点调试上下文
    /// </summary>
    public struct BTNodeDebugContext
    {
        /// <summary> 关联黑板 </summary>
        public IBlackboard bb;
    }
}

#endif