namespace Verve.Unit
{
    /// <summary>
    /// 自定义单元
    /// </summary>
    [System.Obsolete("Unit system is deprecated, please use the GameFeatures system")]
    public interface ICustomUnit : System.IDisposable
    {
        /// <summary>
        /// 单元名
        /// </summary>
        string UnitName { get; }
        
        /// <summary>
        /// 优先级
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 允许每帧运行，默认关闭每帧运行
        /// </summary>
        bool CanEverTick { get; }

        /// <summary>
        /// 单元启用，由 UnitRules 管理生命周期
        /// </summary>
        /// <param name="args">初始化传入参数</param>
        void Startup(UnitRules parent, params object[] args);
        
        /// <summary>
        /// 每帧运行，由 UnitRules 管理生命周期
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="unscaledTime"></param>
        void Tick(float deltaTime, float unscaledTime);
        
        /// <summary>
        /// 单元卸载，由 UnitRules 管理生命周期
        /// </summary>
        void Shutdown();
    }
}