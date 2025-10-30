namespace Verve
{
    /// <summary>
    ///   <para>游戏功能子模块可更新接口</para>
    ///   <para>用于更新游戏功能子模块逻辑</para>
    /// </summary>
    public interface ITickableGameFeatureSubmodule : IGameFeatureSubmodule
    {
        /// <summary>
        ///   <para>每帧执行子模块逻辑</para>
        /// </summary>
        /// <param name="context">游戏功能上下文</param>
        void Tick(in IGameFeatureContext context);
    }
}