namespace Verve
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    
    /// <summary>
    ///   <para>模块生命周期上下文。</para>
    /// </summary>
    public sealed class GameModuleContext
    {
        /// <summary>
        ///   <para>延迟 Tick 注册的应用结果；后续容器写入失败时由上层撤销。</para>
        /// </summary>
        internal sealed class AppliedTickRegistrations
        {
            /// <summary>
            ///   <para>模块容器。</para>
            /// </summary>
            private readonly GameModules m_Modules;
            /// <summary>
            ///   <para>所属模块。</para>
            /// </summary>
            private readonly IGameModule m_OwnerModule;
            /// <summary>
            ///   <para>已应用系统。</para>
            /// </summary>
            private List<object> m_AppliedSystems;

            /// <summary>
            ///   <para>创建 Tick 注册应用结果。</para>
            /// </summary>
            /// <param name="modules">模块。</param>
            /// <param name="ownerModule">所属模块。</param>
            /// <param name="systems">系统。</param>
            internal AppliedTickRegistrations(
                GameModules modules,
                IGameModule ownerModule,
                List<object> systems)
            {
                m_Modules = modules ?? throw new ArgumentNullException(nameof(modules));
                m_OwnerModule = ownerModule ?? throw new ArgumentNullException(nameof(ownerModule));
                m_AppliedSystems = systems;
            }

            /// <summary>
            ///   <para>确认 Tick 注册已经成为容器状态的一部分，不再允许撤销。</para>
            /// </summary>
            internal void Accept() => m_AppliedSystems = null;

            /// <summary>
            ///   <para>撤销已经写入调度器的 Tick 对象。</para>
            /// </summary>
            internal Exception Revert()
            {
                if (m_AppliedSystems == null || m_AppliedSystems.Count == 0) return null;

                List<Exception> errors = null;
                for (int i = m_AppliedSystems.Count - 1; i >= 0; i--)
                {
                    var system = m_AppliedSystems[i];
                    try
                    {
                        if (!m_Modules.RemoveTickSystemFromContext(m_OwnerModule, system))
                        {
                            throw new InvalidOperationException(
                                $"Applied tick system was not found while reverting install state. type={GameModuleUtility.GetTypeDisplayName(system?.GetType())}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionUtility.Add(ref errors, ex);
                    }
                }

                m_AppliedSystems = null;
                return ExceptionUtility.Combine(errors);
            }
        }

        /// <summary>
        ///   <para>所属的模块容器。</para>
        /// </summary>
        private readonly GameModules m_Modules;

        /// <summary>
        ///   <para>当前正在执行生命周期回调的模块实例。</para>
        /// </summary>
        private readonly IGameModule m_OwnerModule;

        /// <summary>
        ///   <para>上下文是否仍然可用。</para>
        /// </summary>
        [NonSerialized] private int m_IsActive = 1;

        /// <summary>
        ///   <para>是否延迟应用 Tick 注册。</para>
        /// </summary>
        [NonSerialized] private readonly bool m_DeferTickRegistration;

        /// <summary>
        ///   <para>等待应用的 Tick 对象。</para>
        /// </summary>
        [NonSerialized] private List<object> m_PendingTickSystems;

        /// <summary>
        ///   <para>创建模块上下文。</para>
        /// </summary>
        /// <param name="modules">模块。</param>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="deferTickRegistration">是否延迟登记 Tick 对象。</param>
        internal GameModuleContext(GameModules modules, IGameModule ownerModule, bool deferTickRegistration = false)
        {
            m_Modules = modules ?? throw new ArgumentNullException(nameof(modules));
            m_OwnerModule = ownerModule ?? throw new ArgumentNullException(nameof(ownerModule));
            m_DeferTickRegistration = deferTickRegistration;
        }

        /// <summary>
        ///   <para>获取当前模块显式声明的依赖模块。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public T GetDependency<T>()
            where T : class, IGameModule
        {
            EnsureActive();
            EnsureDependencyDeclared(typeof(T));
            return m_Modules.GetDependencyFromContext<T>(m_OwnerModule);
        }

        /// <summary>
        ///   <para>尝试获取已声明的必需依赖；不用于查询可选模块。</para>
        /// </summary>
        /// <param name="dependency">匹配到的依赖模块实例。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public bool TryGetDependency<T>(out T dependency)
            where T : class, IGameModule
        {
            EnsureActive();
            EnsureDependencyDeclared(typeof(T));
            return m_Modules.TryGetDependencyFromContext(m_OwnerModule, out dependency);
        }

        /// <summary>
        ///   <para>注册模块拥有的 Tick 对象。</para>
        /// </summary>
        /// <param name="system">要加入调度器的 Tick 对象。</param>
        public void AddTickSystem(object system)
        {
            EnsureActive();
            if (m_DeferTickRegistration)
            {
                m_Modules.ValidateTickSystemFromContext(m_OwnerModule, system);
                m_PendingTickSystems ??= new List<object>();
                if (!ContainsPendingTickSystem(system))
                {
                    m_PendingTickSystems.Add(system);
                }

                return;
            }

            m_Modules.AddTickSystemFromContext(m_OwnerModule, system);
        }

        /// <summary>
        ///   <para>移除模块拥有的 Tick 对象。</para>
        /// </summary>
        /// <param name="system">要从调度器中移除的 Tick 对象。</param>
        public bool RemoveTickSystem(object system)
        {
            EnsureActive();
            if (m_DeferTickRegistration && RemovePendingTickSystem(system))
            {
                return true;
            }

            return m_Modules.RemoveTickSystemFromContext(m_OwnerModule, system);
        }

        /// <summary>
        ///   <para>应用延迟注册的 Tick 对象，并返回可撤销的注册结果。</para>
        /// </summary>
        internal AppliedTickRegistrations ApplyPendingTickSystems()
        {
            EnsureActive();
            if (m_PendingTickSystems == null || m_PendingTickSystems.Count == 0)
            {
                return null;
            }

            List<object> appliedSystems = null;
            Exception failure = null;
            try
            {
                for (int i = 0; i < m_PendingTickSystems.Count; i++)
                {
                    var system = m_PendingTickSystems[i];
                    m_Modules.AddTickSystemFromContext(m_OwnerModule, system);
                    appliedSystems ??= new List<object>(m_PendingTickSystems.Count);
                    appliedSystems.Add(system);
                }

                m_PendingTickSystems.Clear();
                m_PendingTickSystems = null;
                return new AppliedTickRegistrations(m_Modules, m_OwnerModule, appliedSystems);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            if (appliedSystems != null)
            {
                var applied = new AppliedTickRegistrations(m_Modules, m_OwnerModule, appliedSystems);
                failure = ExceptionUtility.Combine(failure, applied.Revert());
            }

            m_PendingTickSystems?.Clear();
            m_PendingTickSystems = null;
            ExceptionDispatchInfo.Capture(failure).Throw();
            throw new InvalidOperationException("Failed to apply pending tick systems.", failure);
        }

        /// <summary>
        ///   <para>确保依赖请求已由当前模块类型显式声明。</para>
        /// </summary>
        /// <param name="dependencyType">依赖类型。</param>
        private void EnsureDependencyDeclared(Type dependencyType)
        {
            var ownerType = m_OwnerModule.GetType();
            if (GameModuleDependencyUtility.CanResolveDeclaredDependency(ownerType, dependencyType))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Module {GameModuleUtility.GetTypeDisplayName(ownerType)} attempted to resolve undeclared dependency " +
                $"{GameModuleUtility.GetTypeDisplayName(dependencyType)}. Declare it with {nameof(GameModuleDependencyAttribute)}.");
        }

        /// <summary>
        ///   <para>将上下文标记为失效。</para>
        /// </summary>
        internal void Invalidate()
        {
            m_PendingTickSystems?.Clear();
            m_PendingTickSystems = null;
            Interlocked.Exchange(ref m_IsActive, 0);
        }

        /// <summary>
        ///   <para>确保上下文仍处于有效期。</para>
        /// </summary>
        private void EnsureActive()
        {
            if (Volatile.Read(ref m_IsActive) == 0)
            {
                throw new InvalidOperationException($"{nameof(GameModuleContext)} is no longer valid.");
            }
        }

        /// <summary>
        ///   <para>判断是否包含待注册 Tick 系统。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        private bool ContainsPendingTickSystem(object system)
        {
            if (system == null || m_PendingTickSystems == null) return false;
            for (int i = 0; i < m_PendingTickSystems.Count; i++)
            {
                if (ReferenceEquals(m_PendingTickSystems[i], system)) return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>移除待处理 Tick 系统。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        private bool RemovePendingTickSystem(object system)
        {
            if (system == null || m_PendingTickSystems == null) return false;
            for (int i = 0; i < m_PendingTickSystems.Count; i++)
            {
                if (!ReferenceEquals(m_PendingTickSystems[i], system)) continue;
                m_PendingTickSystems.RemoveAt(i);
                return true;
            }

            return false;
        }
    }
}