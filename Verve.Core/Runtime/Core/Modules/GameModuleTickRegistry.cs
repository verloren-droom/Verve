namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>模块拥有 Tick 对象的归属登记。</para>
    /// </summary>
    internal readonly struct GameModuleTickRegistration
    {
        /// <summary>
        ///   <para>所属模块。</para>
        /// </summary>
        public readonly IGameModule ownerModule;
        /// <summary>
        ///   <para>Tick 系统。</para>
        /// </summary>
        public readonly object tickSystem;

        /// <summary>
        ///   <para>创建模块 Tick 登记。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="tickSystem">Tick 系统。</param>
        public GameModuleTickRegistration(IGameModule ownerModule, object tickSystem)
        {
            this.ownerModule = ownerModule;
            this.tickSystem = tickSystem;
        }
    }

    /// <summary>
    ///   <para>模块拥有 Tick 对象的注册表。</para>
    /// </summary>
    internal sealed class GameModuleTickRegistry
    {
        /// <summary>
        ///   <para>注册表。</para>
        /// </summary>
        private readonly GameModuleRegistry m_Registry;
        /// <summary>
        ///   <para>调度器。</para>
        /// </summary>
        private readonly IGameLoopTickSystemScheduler m_Scheduler;
        /// <summary>
        ///   <para>Tick 对象到所属模块的映射。</para>
        /// </summary>
        private readonly Dictionary<object, GameModuleTickRegistration> m_BySystem = new(ReferenceEqualityComparer<object>.Instance);
        /// <summary>
        ///   <para>模块到所注册 Tick 对象的映射。</para>
        /// </summary>
        private readonly Dictionary<IGameModule, HashSet<object>> m_ByModule = new(ReferenceEqualityComparer<IGameModule>.Instance);

        /// <summary>
        ///   <para>创建模块 Tick 注册表。</para>
        /// </summary>
        /// <param name="registry">注册表。</param>
        /// <param name="scheduler">调度器。</param>
        public GameModuleTickRegistry(GameModuleRegistry registry, IGameLoopTickSystemScheduler scheduler)
        {
            m_Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            m_Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        /// <summary>
        ///   <para>注册模块拥有的 Tick 对象。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        internal void Add(IGameModule ownerModule, object system)
        {
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            if (system == null) throw new ArgumentNullException(nameof(system));
            ValidateTickSystem(system, requireTickInterfaces: true);

            lock (m_Registry.SyncRoot)
            {
                ValidateAddNoLock(ownerModule, system);
                m_Scheduler.AddSystem(system);
                TrackNoLock(ownerModule, system);
            }
        }

        /// <summary>
        ///   <para>校验模块拥有的 Tick 对象是否可注册。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        internal void ValidateAdd(IGameModule ownerModule, object system)
        {
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            if (system == null) throw new ArgumentNullException(nameof(system));
            ValidateTickSystem(system, requireTickInterfaces: true);

            lock (m_Registry.SyncRoot)
            {
                ValidateAddNoLock(ownerModule, system);
            }
        }

        /// <summary>
        ///   <para>移除模块拥有的 Tick 对象。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        internal bool Remove(IGameModule ownerModule, object system)
        {
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            if (system == null) return false;
            ValidateTickSystem(system, requireTickInterfaces: false);

            lock (m_Registry.SyncRoot)
            {
                if (!m_BySystem.TryGetValue(system, out var ownership))
                {
                    return false;
                }

                if (!ReferenceEquals(ownership.ownerModule, ownerModule))
                {
                    throw new InvalidOperationException("Tick system is owned by another module and cannot be removed through this context.");
                }

                RemoveTrackedSystem(system, "module context removal");
                UntrackNoLock(ownerModule, system);
                return true;
            }
        }

        /// <summary>
        ///   <para>复制指定模块当前拥有的 Tick 对象登记。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="output">接收结果的集合。</param>
        internal void CopyTo(IGameModule ownerModule, List<GameModuleTickRegistration> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (ownerModule == null) return;

            lock (m_Registry.SyncRoot)
            {
                if (!m_ByModule.TryGetValue(ownerModule, out var ownedSystems) || ownedSystems.Count == 0)
                {
                    return;
                }

                foreach (var ownedSystem in ownedSystems)
                {
                    if (ownedSystem == null) continue;
                    if (!m_BySystem.TryGetValue(ownedSystem, out var registration)) continue;
                    if (!ReferenceEquals(registration.ownerModule, ownerModule)) continue;

                    output.Add(registration);
                }
            }
        }

        /// <summary>
        ///   <para>终止全部 Tick 登记；逐项处理后汇总错误。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        internal void Detach(IGameModule ownerModule)
        {
            lock (m_Registry.SyncRoot)
            {
                if (!m_ByModule.TryGetValue(ownerModule, out var systems)) return;
                m_ByModule.Remove(ownerModule);
                List<Exception> errors = null;
                foreach (var system in systems)
                {
                    m_BySystem.Remove(system);
                    try { RemoveTrackedSystem(system, "detaching owned tick systems"); }
                    catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
                }
                ExceptionUtility.ThrowIfAny(errors);
            }
        }

        /// <summary>
        ///   <para>返回对象支持的 Tick 相位文本。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        internal static string GetTickPhasesText(object system)
        {
            if (system == null) return GameModuleUtility.NoneText;

            List<string> phases = null;

            if (system is IEarlyTick)
            {
                phases ??= new List<string>(4);
                phases.Add(TickGroup.Early.ToString());
            }
            if (system is IPhysicsTick)
            {
                phases ??= new List<string>(4);
                phases.Add(TickGroup.Physics.ToString());
            }

            if (system is IGameplayTick)
            {
                phases ??= new List<string>(4);
                phases.Add(TickGroup.Gameplay.ToString());
            }

            if (system is ILateTick)
            {
                phases ??= new List<string>(4);
                phases.Add(TickGroup.Late.ToString());
            }

            return phases == null
                ? GameModuleUtility.NoneText
                : string.Join(GameModuleUtility.TextListSeparator, phases);
        }

        /// <summary>
        ///   <para>校验添加。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        private void ValidateAddNoLock(IGameModule ownerModule, object system)
        {
            if (m_Registry.IsUninstalling(ownerModule))
            {
                throw new InvalidOperationException("Cannot add tick systems while the owner module is uninstalling.");
            }

            if (m_BySystem.TryGetValue(system, out var existingOwnership)
                && !ReferenceEquals(existingOwnership.ownerModule, ownerModule))
            {
                throw new InvalidOperationException("Tick system is already owned by another module.");
            }
        }

        /// <summary>
        ///   <para>校验 Tick 系统。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        /// <param name="requireTickInterfaces">是否要求实现 Tick 接口。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateTickSystem(object system, bool requireTickInterfaces)
        {
            if (system is GameModule module)
            {
                throw new InvalidOperationException(
                    $"Owned tick objects cannot be {nameof(GameModule)} instances. Register a separate runtime object instead. type={GameModuleUtility.GetTypeDisplayName(module.GetType())}");
            }

            if (requireTickInterfaces && !GameModuleUtility.HasTickInterfaces(system))
            {
                throw new InvalidOperationException(
                    $"Tick system must implement {GameModuleUtility.GetTickInterfaceNames()}. type={GameModuleUtility.GetTypeDisplayName(system?.GetType())}");
            }
        }

        /// <summary>
        ///   <para>登记。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        private void TrackNoLock(IGameModule ownerModule, object system)
        {
            m_BySystem[system] = new GameModuleTickRegistration(ownerModule, system);
            if (!m_ByModule.TryGetValue(ownerModule, out var ownedSystems))
            {
                ownedSystems = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
                m_ByModule[ownerModule] = ownedSystems;
            }

            ownedSystems.Add(system);
        }

        /// <summary>
        ///   <para>移除登记。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        private void UntrackNoLock(IGameModule ownerModule, object system)
        {
            m_BySystem.Remove(system);
            if (!m_ByModule.TryGetValue(ownerModule, out var ownedSystems))
            {
                return;
            }

            ownedSystems.Remove(system);
            if (ownedSystems.Count == 0)
            {
                m_ByModule.Remove(ownerModule);
            }
        }

        /// <summary>
        ///   <para>移除已登记系统。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        /// <param name="operation">操作。</param>
        private void RemoveTrackedSystem(object system, string operation)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (m_Scheduler.RemoveSystem(system)) return;

            throw new InvalidOperationException(
                $"Tracked tick system could not be removed from scheduler. operation={operation}, type={GameModuleUtility.GetTypeDisplayName(system.GetType())}");
        }
    }
}