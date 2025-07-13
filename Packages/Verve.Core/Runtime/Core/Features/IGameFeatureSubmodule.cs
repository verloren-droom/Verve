namespace Verve
{
    /// <summary>
    /// 游戏功能子模块接口
    /// </summary>
    public interface IGameFeatureSubmodule
    {
        string ModuleName { get; }
        void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies);
        void OnModuleUnloaded();
    }
}