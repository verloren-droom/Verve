namespace Verve
{
    using System;

    /// <summary>
    ///   <para>模块操作类型。</para>
    /// </summary>
    public enum GameModuleOperation : byte
    {
        /// <summary>
        ///   <para>安装。</para>
        /// </summary>
        Install,
        /// <summary>
        ///   <para>卸载。</para>
        /// </summary>
        Uninstall,
    }

    /// <summary>
    ///   <para>模块操作结果；清单后续失败可能触发已安装模块的卸载结果。</para>
    /// </summary>
    public readonly struct GameModuleOperationResult
    {
        /// <summary>
        ///   <para>模块类型；无类型工厂在返回实例前失败时为空。</para>
        /// </summary>
        public Type ModuleType { get; }
        /// <summary>
        ///   <para>操作。</para>
        /// </summary>
        public GameModuleOperation Operation { get; }
        /// <summary>
        ///   <para>操作耗时。</para>
        /// </summary>
        public TimeSpan Duration { get; }
        /// <summary>
        ///   <para>操作错误；包含清理错误，不包含观察者错误。</para>
        /// </summary>
        public Exception Failure { get; }

        /// <summary>
        ///   <para>创建模块操作结果。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="operation">操作。</param>
        /// <param name="duration">操作耗时。</param>
        /// <param name="failure">操作错误。</param>
        internal GameModuleOperationResult(Type moduleType, GameModuleOperation operation, TimeSpan duration, Exception failure)
        {
            ModuleType = moduleType;
            Operation = operation;
            Duration = duration;
            Failure = failure;
        }
    }

    /// <summary>
    ///   <para>模块操作观察者；接收完成结果，由容器独占并最后释放。</para>
    /// </summary>
    public interface IGameModuleObserver : IDisposable
    {
        /// <summary>
        ///   <para>接收操作结果；回调中不得变更容器。</para>
        /// </summary>
        /// <param name="result">已完成的模块操作结果。</param>
        void OnCompleted(GameModuleOperationResult result);
    }
}