#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    /// <summary>
    /// 节点调试可视化接口
    /// </summary>
    public interface IBTNodeDebugVisualizer : IBTNodeDebuggable
    {
        void DrawDebugGUI();
        void DrawGizmos();
    }
}

#endif