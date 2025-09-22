#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    /// <summary>
    /// 可绘制子模块接口
    /// </summary>
    public interface IDrawableSubmodule : IGameFeatureSubmodule
    {
        /// <summary>
        /// 绘制 IMGUI
        /// </summary>
        void DrawGUI();

        /// <summary>
        /// 绘制 Gizmos
        /// </summary>
        void DrawGizmos();
    }
}

#endif