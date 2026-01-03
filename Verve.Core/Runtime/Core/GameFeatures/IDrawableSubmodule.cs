namespace Verve
{
    /// <summary>
    ///   <para>可绘制子模块接口</para>
    /// </summary>
    public interface IDrawableSubmodule : IGameFeatureSubmodule
    {
        /// <summary>
        ///   <para>绘制IMGUI</para>
        /// </summary>
        void DrawGUI();

        /// <summary>
        ///   <para>绘制Gizmos</para>
        /// </summary>
        void DrawGizmos();
    }
}