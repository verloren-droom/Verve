namespace Verve
{
    /// <summary>
    /// 游戏功能子模块接口 - 用于功能具体逻辑的实现，在同一个模块中，多个功能子模块之间不能有依赖关系，且没有顺序之分，可并行处理
    /// </summary>
    public interface IGameFeatureSubmodule
    {
        /// <summary>
        /// 获取或设置子模块的启用状态
        /// </summary>
        bool IsEnabled { get; set; }
        /// <summary> 启动子模块 </summary>
        void Startup();
        /// <summary> 关闭子模块 </summary>
        void Shutdown();
    }
}