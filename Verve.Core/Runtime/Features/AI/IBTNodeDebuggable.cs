namespace Verve.AI
{
    /// <summary>
    ///   <para>可被调试的行为树节点</para>
    /// </summary>
    public interface IBTNodeDebuggable
    {
        /// <summary>
        ///   <para>是否被调试</para>
        /// </summary>
        bool IsDebug { get; set; }
    }

    
    /// <summary>
    ///   <para>节点调试上下文</para>
    /// </summary>
    public struct BTNodeDebugContext
    {
        /// <summary>
        ///   <para>关联黑板</para>
        /// </summary>
        public IBlackboard bb;
    }
}