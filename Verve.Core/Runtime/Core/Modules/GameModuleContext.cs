namespace Verve
{
    using System;
    using System.Threading;


    /// <summary>
    ///   <para>模块生命周期上下文</para>
    /// </summary>
    [Serializable]
    public sealed class GameModuleContext
    {
        /// <summary>
        ///   <para>所属的模块容器</para>
        /// </summary>
        private readonly GameModules m_Modules;

        /// <summary>
        ///   <para>当前正在执行生命周期回调的模块实例</para>
        /// </summary>
        private readonly IGameModule m_OwnerModule;

        /// <summary>
        ///   <para>上下文是否仍然可用</para>
        /// </summary>
        [NonSerialized] private int m_IsActive = 1;

        internal GameModuleContext(GameModules modules, IGameModule ownerModule)
        {
            m_Modules = modules ?? throw new ArgumentNullException(nameof(modules));
            m_OwnerModule = ownerModule ?? throw new ArgumentNullException(nameof(ownerModule));
        }

        /// <summary>
        ///   <para>当前生命周期回调所属的模块容器</para>
        /// </summary>
        public GameModules Modules
        {
            get
            {
                EnsureActive();
                return m_Modules;
            }
        }

        /// <summary>
        ///   <para>获取已安装模块</para>
        /// </summary>
        public T GetModule<T>()
            where T : class, IGameModule
        {
            EnsureActive();
            return m_Modules.GetModuleFromContext<T>();
        }

        /// <summary>
        ///   <para>尝试获取已安装模块</para>
        /// </summary>
        /// <param name="module">匹配到的模块实例</param>
        public bool TryGetModule<T>(out T module)
            where T : class, IGameModule
        {
            EnsureActive();
            return m_Modules.TryGetModuleFromContext(out module);
        }

        /// <summary>
        ///   <para>注册模块拥有的 Tick 对象</para>
        /// </summary>
        /// <param name="system">要加入调度器的 Tick 对象</param>
        /// <param name="runInBackground">是否将该对象放到后台线程执行</param>
        public void AddTickSystem(object system, bool runInBackground = false)
        {
            EnsureActive();
            m_Modules.AddTickSystemFromContext(m_OwnerModule, system, runInBackground);
        }

        /// <summary>
        ///   <para>移除模块拥有的 Tick 对象</para>
        /// </summary>
        /// <param name="system">要从调度器中移除的 Tick 对象</param>
        public bool RemoveTickSystem(object system)
        {
            EnsureActive();
            return m_Modules.RemoveTickSystemFromContext(m_OwnerModule, system);
        }

        /// <summary>
        ///   <para>将上下文标记为失效</para>
        /// </summary>
        internal void Invalidate()
        {
            Interlocked.Exchange(ref m_IsActive, 0);
        }

        /// <summary>
        ///   <para>确保上下文仍处于有效期</para>
        /// </summary>
        private void EnsureActive()
        {
            if (Volatile.Read(ref m_IsActive) == 0)
            {
                throw new InvalidOperationException($"{nameof(GameModuleContext)} is no longer valid.");
            }
        }
    }
}