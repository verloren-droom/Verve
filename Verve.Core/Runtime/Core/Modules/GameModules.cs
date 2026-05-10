namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    /// <summary>
    ///   <para>模块容器</para>
    ///   <para>负责托管一组模块实例、依赖关系和 Tick 调度器</para>
    /// </summary>
    [Serializable]
    public sealed class GameModules : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///   <para>容器释放状态</para>
        /// </summary>
        private enum DisposeState : byte
        {
            /// <summary>
            ///   <para>正常运行中</para>
            /// </summary>
            Alive = 0,
            /// <summary>
            ///   <para>正在释放过程中</para>
            /// </summary>
            Disposing = 1,
            /// <summary>
            ///   <para>已完成释放</para>
            /// </summary>
            Disposed = 2,
        }

        /// <summary>
        ///   <para>一次模块变更的句柄</para>
        /// </summary>
        internal readonly struct ChangeScope : IDisposable
        {
            private readonly GameModules m_Owner;

            public ChangeScope(GameModules owner)
            {
                m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                owner.BeginChange();
            }

            public void Dispose()
            {
                m_Owner.EndChange();
            }
        }

        /// <summary>
        ///   <para>模块拥有 Tick 对象的归属登记</para>
        /// </summary>
        internal readonly struct OwnedTickRegistration
        {
            /// <summary>
            ///   <para>拥有该 Tick 对象的模块实例</para>
            /// </summary>
            public readonly IGameModule ownerModule;
            /// <summary>
            ///   <para>被注册到调度器的 Tick 对象</para>
            /// </summary>
            public readonly object tickSystem;
            /// <summary>
            ///   <para>该 Tick 对象是否以后台线程模式执行</para>
            /// </summary>
            public readonly bool runInBackground;

            public OwnedTickRegistration(IGameModule ownerModule, object tickSystem, bool runInBackground)
            {
                this.ownerModule = ownerModule;
                this.tickSystem = tickSystem;
                this.runInBackground = runInBackground;
            }
        }

        /// <summary>
        ///   <para>保护全局模块实例所有权映射的同步锁</para>
        /// </summary>
        private static readonly object s_ModuleOwnershipLock = new();

        /// <summary>
        ///   <para>模块实例到所属容器的全局映射</para>
        ///   <para>用于阻止同一模块实例被多个容器同时安装</para>
        /// </summary>
        private static readonly Dictionary<IGameModule, GameModules> s_ModuleOwners = new(GameModuleUtility.ReferenceComparer<IGameModule>.Instance);

        /// <summary>
        ///   <para>模块注册表</para>
        /// </summary>
        private readonly GameModuleRegistry m_Registry = new();

        /// <summary>
        ///   <para>当前容器使用的 Tick 调度器</para>
        /// </summary>
        private readonly ITickSystemScheduler m_Scheduler;

        /// <summary>
        ///   <para>模块生命周期执行器</para>
        /// </summary>
        private readonly GameModuleLifecycleRunner m_LifecycleRunner;

        /// <summary>
        ///   <para>Tick 执行与容器释放之间的并发屏障</para>
        /// </summary>
        private readonly object m_TickBarrierLock = new();

        /// <summary>
        ///   <para>Tick 对象到所有者模块的反向索引</para>
        /// </summary>
        private readonly Dictionary<object, OwnedTickRegistration> m_OwnedTickBySystem = new(GameModuleUtility.ReferenceComparer<object>.Instance);

        /// <summary>
        ///   <para>模块拥有的 Tick 对象索引</para>
        /// </summary>
        private readonly Dictionary<IGameModule, HashSet<object>> m_OwnedTicksByModule = new(GameModuleUtility.ReferenceComparer<IGameModule>.Instance);

        /// <summary>
        ///   <para>当前正在执行中的 Tick 调用数量</para>
        /// </summary>
        private int m_ActiveTickCount;

        /// <summary>
        ///   <para>当前是否有容器级模块变更正在进行</para>
        /// </summary>
        private int m_IsChanging;

        /// <summary>
        ///   <para>当前变更流程是否积累了待发送的模块集合变更通知</para>
        /// </summary>
        private int m_ModulesChangedQueued;

        /// <summary>
        ///   <para>容器释放状态机</para>
        /// </summary>
        private int m_DisposeState;

        /// <summary>
        ///   <para>是否已释放</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_DisposeState) == (int)DisposeState.Disposed;
        }

        /// <summary>
        ///   <para>是否正在释放</para>
        /// </summary>
        public bool IsDisposing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_DisposeState) == (int)DisposeState.Disposing;
        }

        /// <summary>
        ///   <para>容器是否已不可再对外提供稳定访问</para>
        /// </summary>
        internal bool IsDisposedOrDisposing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive;
        }

        /// <summary>
        ///   <para>是否正在执行容器级模块变更</para>
        /// </summary>
        public bool IsChanging
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_IsChanging) != 0;
        }

        /// <summary>
        ///   <para>当前所有已安装模块列表</para>
        /// </summary>
        public IReadOnlyList<IGameModule> InstalledModules
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfNotAlive();
                return m_Registry.ModuleList;
            }
        }

        /// <summary>
        ///   <para>当模块集合发生变化时触发</para>
        /// </summary>
        public event Action OnModulesChanged;

        public GameModules(ITickSystemScheduler scheduler = null)
        {
            m_Scheduler = scheduler ?? new TickSystemScheduler();
            m_LifecycleRunner = new GameModuleLifecycleRunner(this, m_Registry);
        }

        /// <summary>
        ///   <para>将当前所有模块复制到指定列表中</para>
        /// </summary>
        /// <param name="output">用于接收模块副本的列表</param>
        public void CopyModulesTo(List<IGameModule> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            ThrowIfNotAlive();
            m_Registry.CopyModulesTo(output);
        }

        /// <summary>
        ///   <para>获取已安装模块</para>
        /// </summary>
        public T GetModule<T>()
            where T : class, IGameModule
        {
            ThrowIfNotAlive();
            return m_Registry.GetModule<T>();
        }

        /// <summary>
        ///   <para>尝试获取已安装模块</para>
        /// </summary>
        /// <param name="module">匹配到的模块实例</param>
        public bool TryGetModule<T>(out T module)
            where T : class, IGameModule
        {
            if (IsDisposedOrDisposing)
            {
                module = null;
                return false;
            }

            return TryGetModuleNoThrow(out module);
        }

        /// <summary>
        ///   <para>供生命周期上下文在容器释放期间继续读取模块依赖</para>
        /// </summary>
        internal T GetModuleFromContext<T>()
            where T : class, IGameModule
        {
            return m_Registry.GetModule<T>();
        }

        /// <summary>
        ///   <para>供生命周期上下文在容器释放期间继续尝试读取模块依赖</para>
        /// </summary>
        internal bool TryGetModuleFromContext<T>(out T module)
            where T : class, IGameModule
        {
            return TryGetModuleNoThrow(out module);
        }

        /// <summary>
        ///   <para>使用无参构造函数同步创建并安装模块</para>
        /// </summary>
        public void Install<T>()
            where T : GameModule, new()
        {
            m_LifecycleRunner.Install(static () => new T());
        }

        /// <summary>
        ///   <para>使用无参构造函数异步创建并安装模块</para>
        /// </summary>
        public ValueTask InstallAsync<T>()
            where T : GameModule, new()
        {
            return m_LifecycleRunner.InstallAsync(static () => new T());
        }

        /// <summary>
        ///   <para>使用模块创建工厂同步安装模块</para>
        /// </summary>
        /// <param name="factory">模块创建工厂</param>
        public void Install(Func<GameModule> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            m_LifecycleRunner.Install(factory);
        }

        /// <summary>
        ///   <para>使用模块创建工厂异步安装模块</para>
        /// </summary>
        /// <param name="factory">模块创建工厂</param>
        public ValueTask InstallAsync(Func<GameModule> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return m_LifecycleRunner.InstallAsync(factory);
        }
        
        /// <summary>
        ///   <para>按清单自身配置的安装顺序策略安装模块</para>
        /// </summary>
        /// <param name="manifest">要执行的模块清单</param>
        public void InstallFromManifest(GameModuleManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            m_LifecycleRunner.InstallFromManifest(manifest);
        }

        /// <summary>
        ///   <para>按清单自身配置的安装顺序策略异步安装模块</para>
        /// </summary>
        /// <param name="manifest">要执行的模块清单</param>
        public ValueTask InstallFromManifestAsync(GameModuleManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            return m_LifecycleRunner.InstallFromManifestAsync(manifest);
        }

        /// <summary>
        ///   <para>卸载指定精确类型的模块</para>
        /// </summary>
        /// <param name="dispose">卸载完成后是否调用模块的 <see cref="IDisposable.Dispose"/></param>
        public bool Uninstall<T>(bool dispose = true)
            where T : GameModule
        {
            return m_LifecycleRunner.Uninstall(typeof(T), dispose);
        }

        /// <summary>
        ///   <para>异步卸载入口</para>
        /// </summary>
        /// <param name="dispose">卸载完成后是否调用模块的 <see cref="IDisposable.Dispose"/></param>
        public ValueTask<bool> UninstallAsync<T>(bool dispose = true)
            where T : GameModule
        {
            return m_LifecycleRunner.UninstallAsync(typeof(T), dispose);
        }

        /// <summary>
        ///   <para>按逆安装顺序卸载所有模块</para>
        /// </summary>
        /// <param name="dispose">卸载完成后是否调用模块的 <see cref="IDisposable.Dispose"/></param>
        public void UninstallAll(bool dispose = true)
        {
            m_LifecycleRunner.UninstallAll(dispose);
        }

        /// <summary>
        ///   <para>异步卸载全部入口</para>
        /// </summary>
        /// <param name="dispose">卸载完成后是否调用模块的 <see cref="IDisposable.Dispose"/></param>
        public ValueTask UninstallAllAsync(bool dispose = true)
        {
            return m_LifecycleRunner.UninstallAllAsync(dispose);
        }

        /// <summary>
        ///   <para>驱动当前容器的 Tick 调度</para>
        /// </summary>
        /// <param name="deltaTime">当前 Tick 阶段的时间步长</param>
        /// <param name="group">要执行的 Tick 分组</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float deltaTime, TickGroup group)
        {
            if (!TryEnterTick()) return;

            try
            {
                m_Scheduler.Tick(deltaTime, group);
            }
            finally
            {
                ExitTick();
            }
        }

        /// <summary>
        ///   <para>释放模块容器</para>
        /// </summary>
        public void Dispose()
        {
            if (!PrepareDispose()) return;
            FinishDispose();
        }

        /// <summary>
        ///   <para>异步释放模块容器</para>
        /// </summary>
        public ValueTask DisposeAsync()
        {
            if (!PrepareDispose()) return default;
            return FinishDisposeAsync();
        }

        /// <summary>
        ///   <para>为指定模块创建生命周期上下文</para>
        /// </summary>
        /// <param name="module">即将执行生命周期回调的模块实例</param>
        internal GameModuleContext CreateContext(IGameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (IsDisposed) throw new ObjectDisposedException(nameof(GameModules));
            return new GameModuleContext(this, module);
        }

        /// <summary>
        ///   <para>通知模块集合变更</para>
        /// </summary>
        internal void NotifyModulesChanged()
        {
            if (Volatile.Read(ref m_IsChanging) != 0)
            {
                Interlocked.Exchange(ref m_ModulesChangedQueued, 1);
                return;
            }

            GameModuleUtility.InvokeEvent(OnModulesChanged, nameof(OnModulesChanged));
        }

        /// <summary>
        ///   <para>不抛出容器状态异常的模块尝试解析流程</para>
        /// </summary>
        private bool TryGetModuleNoThrow<T>(out T module)
            where T : class, IGameModule
        {
            if (m_Registry.TryGetModule(typeof(T), out var resolved) && resolved is T typedModule)
            {
                module = typedModule;
                return true;
            }

            module = null;
            return false;
        }

        /// <summary>
        ///   <para>进入统一释放流程的公共前置阶段</para>
        /// </summary>
        private bool PrepareDispose()
        {
            if (!TryBeginDispose()) return false;

            try
            {
                m_Scheduler.WaitForIdle();
            }
            catch
            {
                ResetDisposeStateToAlive();
                throw;
            }

            m_Registry.ClearTransitionState();

            return true;
        }

        /// <summary>
        ///   <para>执行同步释放收尾并聚合错误</para>
        /// </summary>
        private void FinishDispose()
        {
            List<Exception> errors = null;
            try
            {
                try
                {
                    m_LifecycleRunner.DisposeAll();
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }

                try
                {
                    m_Scheduler.Dispose();
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }
            finally
            {
                FinalizeDisposeState();
            }

            ThrowDisposeErrors(errors);
        }

        /// <summary>
        ///   <para>执行异步释放收尾并聚合错误</para>
        /// </summary>
        private async ValueTask FinishDisposeAsync()
        {
            List<Exception> errors = null;
            try
            {
                try
                {
                    await m_LifecycleRunner.DisposeAllAsync();
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }

                try
                {
                    m_Scheduler.Dispose();
                }
                catch (Exception ex)
                {
                    GameModuleUtility.AddError(ref errors, ex);
                }
            }
            finally
            {
                FinalizeDisposeState();
            }

            ThrowDisposeErrors(errors);
        }

        /// <summary>
        ///   <para>通过生命周期上下文为模块注册拥有的 Tick 对象</para>
        /// </summary>
        /// <param name="ownerModule">请求注册的模块所有者</param>
        /// <param name="system">要注册的 Tick 对象</param>
        /// <param name="runInBackground">是否后台执行</param>
        internal void AddTickSystemFromContext(IGameModule ownerModule, object system, bool runInBackground)
        {
            ThrowIfNotAlive();
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            if (system == null) throw new ArgumentNullException(nameof(system));
            ValidateOwnedTickSystem(system, true);
            lock (m_Registry.SyncRoot)
            {
                if (m_Registry.IsUninstalling(ownerModule))
                {
                    throw new InvalidOperationException("Cannot add tick systems while the owner module is uninstalling.");
                }

                if (m_OwnedTickBySystem.TryGetValue(system, out var existingOwnership)
                    && !ReferenceEquals(existingOwnership.ownerModule, ownerModule))
                {
                    throw new InvalidOperationException("Tick system is already owned by another module.");
                }

                m_Scheduler.AddSystem(system, runInBackground);
                TrackOwnedTickNoLock(ownerModule, system, runInBackground);
            }
        }

        /// <summary>
        ///   <para>通过生命周期上下文移除模块注册的 Tick 对象</para>
        /// </summary>
        /// <param name="ownerModule">请求移除的模块所有者</param>
        /// <param name="system">要移除的 Tick 对象</param>
        internal bool RemoveTickSystemFromContext(IGameModule ownerModule, object system)
        {
            if (IsDisposed) return false;
            if (ownerModule == null) throw new ArgumentNullException(nameof(ownerModule));
            if (system == null) return false;
            ValidateOwnedTickSystem(system, false);
            lock (m_Registry.SyncRoot)
            {
                if (!m_OwnedTickBySystem.TryGetValue(system, out var ownership))
                {
                    return false;
                }

                if (!ReferenceEquals(ownership.ownerModule, ownerModule))
                {
                    throw new InvalidOperationException("Tick system is owned by another module and cannot be removed through this context.");
                }

                RemoveTrackedTickSystem(system, "module context removal");
                UntrackOwnedTickNoLock(ownerModule, system);
                return true;
            }
        }

        /// <summary>
        ///   <para>复制指定模块当前拥有的 Tick 对象登记，供运行时调试窗口读取快照</para>
        /// </summary>
        internal void CopyOwnedTickRegistrationsTo(IGameModule ownerModule, List<OwnedTickRegistration> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (ownerModule == null || IsDisposedOrDisposing) return;

            lock (m_Registry.SyncRoot)
            {
                if (!m_OwnedTicksByModule.TryGetValue(ownerModule, out var ownedSystems) || ownedSystems.Count == 0)
                {
                    return;
                }

                foreach (var ownedSystem in ownedSystems)
                {
                    if (ownedSystem == null) continue;
                    if (!m_OwnedTickBySystem.TryGetValue(ownedSystem, out var registration)) continue;
                    if (!ReferenceEquals(registration.ownerModule, ownerModule)) continue;

                    output.Add(registration);
                }
            }
        }

        /// <summary>
        ///   <para>移除指定模块拥有的全部 Tick 对象</para>
        /// </summary>
        internal List<OwnedTickRegistration> DetachOwnedTicks(IGameModule ownerModule)
        {
            if (ownerModule == null) return null;

            lock (m_Registry.SyncRoot)
            {
                if (!m_OwnedTicksByModule.TryGetValue(ownerModule, out var ownedSystems) || ownedSystems.Count == 0)
                {
                    return null;
                }

                var registrations = new List<OwnedTickRegistration>(ownedSystems.Count);
                foreach (var ownedSystem in ownedSystems)
                {
                    if (ownedSystem == null) continue;
                    if (!m_OwnedTickBySystem.TryGetValue(ownedSystem, out var registration)) continue;
                    if (!ReferenceEquals(registration.ownerModule, ownerModule)) continue;

                    registrations.Add(registration);
                }

                if (registrations.Count == 0)
                {
                    m_OwnedTicksByModule.Remove(ownerModule);
                    return null;
                }

                List<OwnedTickRegistration> removedFromScheduler = null;
                Exception failure = null;
                for (int i = 0; i < registrations.Count; i++)
                {
                    var registration = registrations[i];
                    try
                    {
                        RemoveTrackedTickSystem(registration.tickSystem, "detaching owned tick systems");
                        removedFromScheduler ??= new List<OwnedTickRegistration>(registrations.Count);
                        removedFromScheduler.Add(registration);
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                        break;
                    }
                }

                if (failure != null)
                {
                    if (removedFromScheduler != null)
                    {
                        for (int i = removedFromScheduler.Count - 1; i >= 0; i--)
                        {
                            var registration = removedFromScheduler[i];
                            try
                            {
                                m_Scheduler.AddSystem(registration.tickSystem, registration.runInBackground);
                            }
                            catch (Exception rollbackException)
                            {
                                failure = GameModuleUtility.CombineErrors(failure, rollbackException);
                            }
                        }
                    }

                    var moduleName = GameModuleUtility.GetTypeDisplayName(ownerModule.GetType());
                    throw new InvalidOperationException($"Failed to detach owned tick systems for module {moduleName}.", failure);
                }

                for (int i = 0; i < registrations.Count; i++)
                {
                    m_OwnedTickBySystem.Remove(registrations[i].tickSystem);
                }

                m_OwnedTicksByModule.Remove(ownerModule);
                return registrations;
            }
        }

        /// <summary>
        ///   <para>重新挂回先前暂时拆离的 Tick 对象</para>
        /// </summary>
        internal void ReattachOwnedTicks(List<OwnedTickRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0) return;
            if (IsDisposedOrDisposing) return;

            lock (m_Registry.SyncRoot)
            {
                List<OwnedTickRegistration> reattached = null;
                Exception failure = null;
                for (int i = 0; i < registrations.Count; i++)
                {
                    var registration = registrations[i];
                    if (registration.ownerModule == null || registration.tickSystem == null) continue;

                    try
                    {
                        m_Scheduler.AddSystem(registration.tickSystem, registration.runInBackground);
                        TrackOwnedTickNoLock(registration.ownerModule, registration.tickSystem, registration.runInBackground);
                        reattached ??= new List<OwnedTickRegistration>(registrations.Count);
                        reattached.Add(registration);
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                        break;
                    }
                }

                if (failure == null) return;

                if (reattached != null)
                {
                    for (int i = reattached.Count - 1; i >= 0; i--)
                    {
                        var registration = reattached[i];
                        bool removalSucceeded = false;
                        try
                        {
                            RemoveTrackedTickSystem(registration.tickSystem, "rolling back owned tick reattach");
                            removalSucceeded = true;
                        }
                        catch (Exception rollbackException)
                        {
                            failure = GameModuleUtility.CombineErrors(failure, rollbackException);
                        }
                        finally
                        {
                            if (removalSucceeded)
                            {
                                UntrackOwnedTickNoLock(registration.ownerModule, registration.tickSystem);
                            }
                        }
                    }
                }

                throw new InvalidOperationException($"Failed to reattach owned tick objects for module {GameModuleUtility.GetTypeDisplayName(registrations[0].ownerModule?.GetType())}.", failure);
            }
        }

        /// <summary>
        ///   <para>在容器已释放或正在释放时抛出异常</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfNotAlive()
        {
            if (IsDisposedOrDisposing) throw new ObjectDisposedException(nameof(GameModules));
        }

        /// <summary>
        ///   <para>开始一次容器释放流程，并等待所有当前 Tick 退出</para>
        /// </summary>
        private bool TryBeginDispose()
        {
            lock (m_TickBarrierLock)
            {
                if (IsDisposed) return false;
                if (m_IsChanging != 0)
                {
                    throw new InvalidOperationException($"Cannot dispose {nameof(GameModules)} while a module change is in progress.");
                }
                if (TickExecutionScope.IsActive)
                {
                    throw new InvalidOperationException($"Cannot dispose {nameof(GameModules)} from within Tick callbacks. Schedule the disposal after the current Tick.");
                }

                if (Interlocked.CompareExchange(
                        ref m_DisposeState,
                        (int)DisposeState.Disposing,
                        (int)DisposeState.Alive) != (int)DisposeState.Alive)
                {
                    return false;
                }

                while (m_ActiveTickCount > 0)
                {
                    Monitor.Wait(m_TickBarrierLock);
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>把容器释放状态回滚到可运行状态</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetDisposeStateToAlive()
        {
            Volatile.Write(ref m_DisposeState, (int)DisposeState.Alive);
        }

        /// <summary>
        ///   <para>完成容器释放状态提交并通知外部句柄层</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinalizeDisposeState()
        {
            Volatile.Write(ref m_DisposeState, (int)DisposeState.Disposed);
            Game.NotifyModulesDisposed(this);
        }

        /// <summary>
        ///   <para>登记某个模块实例的容器所有权</para>
        /// </summary>
        internal void TakeModuleOwnership(IGameModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            lock (s_ModuleOwnershipLock)
            {
                if (!s_ModuleOwners.TryGetValue(module, out var owner))
                {
                    s_ModuleOwners.Add(module, this);
                    return;
                }

                var moduleTypeName = GameModuleUtility.GetTypeDisplayName(module.GetType());
                if (ReferenceEquals(owner, this))
                {
                    throw new InvalidOperationException(
                        $"Module instance is already owned by this {nameof(GameModules)} container. type={moduleTypeName}");
                }

                throw new InvalidOperationException(
                    $"Module instance is already owned by another {nameof(GameModules)} container. type={moduleTypeName}");
            }
        }

        /// <summary>
        ///   <para>释放某个模块实例的容器所有权</para>
        /// </summary>
        internal void ReleaseModuleOwnership(IGameModule module)
        {
            if (module == null) return;

            lock (s_ModuleOwnershipLock)
            {
                if (s_ModuleOwners.TryGetValue(module, out var owner) && ReferenceEquals(owner, this))
                {
                    s_ModuleOwners.Remove(module);
                }
            }
        }

        /// <summary>
        ///   <para>进入一次容器级模块变更流程</para>
        /// </summary>
        internal ChangeScope EnterChangeScope()
        {
            return new ChangeScope(this);
        }

        /// <summary>
        ///   <para>开始一次容器级模块变更</para>
        /// </summary>
        private void BeginChange()
        {
            lock (m_TickBarrierLock)
            {
                if (IsDisposedOrDisposing) throw new ObjectDisposedException(nameof(GameModules));
                if (m_IsChanging != 0)
                {
                    throw new InvalidOperationException("Concurrent module changes are not supported on the same GameModules container.");
                }
                if (TickExecutionScope.IsActive)
                {
                    throw new InvalidOperationException("Cannot change modules from within Tick callbacks. Schedule the change after the current Tick.");
                }

                m_IsChanging = 1;
                while (m_ActiveTickCount > 0)
                {
                    Monitor.Wait(m_TickBarrierLock);
                }

                if (IsDisposedOrDisposing)
                {
                    m_IsChanging = 0;
                    Monitor.PulseAll(m_TickBarrierLock);
                    throw new ObjectDisposedException(nameof(GameModules));
                }
            }

            try
            {
                m_Scheduler.WaitForIdle();
            }
            catch
            {
                EndChange();
                throw;
            }
        }

        /// <summary>
        ///   <para>结束一次容器级模块变更并恢复 Tick 进入</para>
        /// </summary>
        private void EndChange()
        {
            bool shouldNotify = Interlocked.Exchange(ref m_ModulesChangedQueued, 0) != 0;

            lock (m_TickBarrierLock)
            {
                if (m_IsChanging == 0) return;
                m_IsChanging = 0;
                Monitor.PulseAll(m_TickBarrierLock);
            }

            if (shouldNotify)
            {
                GameModuleUtility.InvokeEvent(OnModulesChanged, nameof(OnModulesChanged));
            }
        }

        /// <summary>
        ///   <para>返回对象支持的 Tick 相位文本</para>
        /// </summary>
        internal static string GetTickPhasesText(object system)
        {
            if (system == null) return GameModuleUtility.NoneText;

            List<string> phases = null;

#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
            if (system is IEarlyTick)
            {
                phases ??= new List<string>(4);
                phases.Add(TickGroup.Early.ToString());
            }
#endif
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
        ///   <para>校验模块拥有的 Tick 对象</para>
        /// </summary>
        /// <param name="system">要校验的对象</param>
        /// <param name="requireTickInterfaces">是否要求对象实现 Tick 接口</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateOwnedTickSystem(object system, bool requireTickInterfaces)
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
        ///   <para>登记一个 Tick 对象的所有权</para>
        /// </summary>
        private void TrackOwnedTickNoLock(IGameModule ownerModule, object system, bool runInBackground)
        {
            m_OwnedTickBySystem[system] = new OwnedTickRegistration(ownerModule, system, runInBackground);
            if (!m_OwnedTicksByModule.TryGetValue(ownerModule, out var ownedSystems))
            {
                ownedSystems = new HashSet<object>(GameModuleUtility.ReferenceComparer<object>.Instance);
                m_OwnedTicksByModule[ownerModule] = ownedSystems;
            }

            ownedSystems.Add(system);
        }

        /// <summary>
        ///   <para>解除一个 Tick 对象的所有权登记</para>
        /// </summary>
        private void UntrackOwnedTickNoLock(IGameModule ownerModule, object system)
        {
            m_OwnedTickBySystem.Remove(system);
            if (!m_OwnedTicksByModule.TryGetValue(ownerModule, out var ownedSystems))
            {
                return;
            }

            ownedSystems.Remove(system);
            if (ownedSystems.Count == 0)
            {
                m_OwnedTicksByModule.Remove(ownerModule);
            }
        }

        /// <summary>
        ///   <para>要求调度器成功移除一个仍被框架跟踪的 Tick 系统</para>
        /// </summary>
        private void RemoveTrackedTickSystem(object system, string operation)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));

            if (m_Scheduler.RemoveSystem(system))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Tracked tick system could not be removed from scheduler. operation={operation}, type={GameModuleUtility.GetTypeDisplayName(system.GetType())}");
        }

        /// <summary>
        ///   <para>尝试进入一次 Tick 执行</para>
        /// </summary>
        private bool TryEnterTick()
        {
            lock (m_TickBarrierLock)
            {
                if (IsDisposedOrDisposing || m_IsChanging != 0) return false;
                m_ActiveTickCount++;
                return true;
            }
        }

        /// <summary>
        ///   <para>结束一次 Tick 执行并在空闲时唤醒等待释放的线程</para>
        /// </summary>
        private void ExitTick()
        {
            lock (m_TickBarrierLock)
            {
                if (m_ActiveTickCount <= 0) return;

                m_ActiveTickCount--;
                if (m_ActiveTickCount == 0)
                {
                    Monitor.PulseAll(m_TickBarrierLock);
                }
            }
        }

        /// <summary>
        ///   <para>把容器释放期间收集到的异常统一抛出</para>
        /// </summary>
        private static void ThrowDisposeErrors(List<Exception> errors)
        {
            if (errors == null || errors.Count == 0) return;
            if (errors.Count == 1)
            {
                throw new InvalidOperationException($"{nameof(GameModules)} disposal failed.", errors[0]);
            }

            throw new AggregateException($"{nameof(GameModules)} disposal failed.", errors);
        }
    }
}