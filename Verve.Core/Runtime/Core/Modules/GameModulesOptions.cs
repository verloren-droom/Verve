namespace Verve
{
    using System;

    /// <summary>
    ///   <para>模块容器选项；创建时确定扩展实现。</para>
    /// </summary>
    public sealed class GameModulesOptions
    {
        /// <summary>
        ///   <para>调度器创建委托；返回容器独占的新实例，由容器释放。</para>
        /// </summary>
        public Func<IGameLoopTickSystemScheduler> CreateScheduler { get; set; }
            = static () => new GameLoopTickSystemScheduler();

        /// <summary>
        ///   <para>模块工厂创建委托；默认工厂调用模块公开无参构造函数。</para>
        /// </summary>
        public Func<IGameModuleFactory> CreateModuleFactory { get; set; }
            = static () => new GameModuleFactory();

        /// <summary>
        ///   <para>观察者创建委托；未配置时不计时或发送通知。</para>
        /// </summary>
        public Func<IGameModuleObserver> CreateObserver { get; set; }
    }
}