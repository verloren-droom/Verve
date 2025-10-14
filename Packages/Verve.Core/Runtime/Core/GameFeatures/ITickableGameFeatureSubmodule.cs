namespace Verve
{
    /// <summary>
    /// 游戏功能子模块可更新接口 - 可用于更新游戏功能子模块逻辑
    /// </summary>
    public interface ITickableGameFeatureSubmodule : IGameFeatureSubmodule
    {
        /// <summary> 每帧执行子模块逻辑 </summary>
        void Tick(in IGameFeatureContext context);
    }
}