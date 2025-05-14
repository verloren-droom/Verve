namespace Verve.Unit
{
    /// <summary>
    /// 自定义单元
    /// </summary>
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
        /// 依赖单元
        /// </summary>
        System.Collections.Generic.HashSet<System.Type> DependencyUnits { get; }
        
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