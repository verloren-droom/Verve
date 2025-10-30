#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    /// <summary>
    ///   <para>节点调试可视化接口</para>
    /// </summary>
    public interface IBTNodeDebugVisualizer : IBTNodeDebuggable
    {
        /// <summary>
        ///   <para>绘制调试GUI</para>
        /// </summary>
        void DrawDebugGUI();
        /// <summary>
        ///   <para>绘制Gizmos</para>
        /// </summary>
        void DrawGizmos();
    }
}

#endif