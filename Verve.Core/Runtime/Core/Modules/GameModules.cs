namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>模块容器；负责托管一组模块实例、依赖关系和 GameLoop 主线程 Tick 调度器。</para>
    /// </summary>
    public sealed class GameModules : IDisposable, IAsyncDisposable
    {
        /// <summary>
        ///   <para>容器释放状态。</para>
        /// </summary>
        private enum DisposeState : byte
        {
            /// <summary>
            ///   <para>正常运行中。</para>
            /// </summary>
            Alive = 0,
            /// <summary>
            ///   <para>正在释放过程中。</para>
            /// </summary>
            Disposing = 1,
            /// <summary>
            ///   <para>已完成释放。</para>
            /// </summary>
            Disposed = 2,
        }

        /// <summary>
        ///   <para>模块变更句柄；释放时结束本次变更。</para>
        /// </summary>
        internal readonly struct ChangeScope : IDisposable
        {
            /// <summary>
            ///   <para>所属容器。</para>
            /// </summary>
            private readonly GameModules m_Owner;
            /// <summary>
            ///   <para>取消令牌。</para>
            /// </summary>
            public readonly CancellationToken cancellationToken;

            /// <summary>
            ///   <para>创建变更作用域。</para>
            /// </summary>
            /// <param name="owner">所属容器。</param>
            /// <param name="ct">取消令牌。</param>
            public ChangeScope(GameModules owner, CancellationToken ct)
            {
                m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                cancellationToken = owner.BeginChange(ct, pauseTicks: true);
            }

            /// <summary>
            ///   <para>创建变更作用域。</para>
            /// </summary>
            /// <param name="cancellationToken">取消令牌。</param>
            /// <param name="owner">所属容器。</param>
            private ChangeScope(CancellationToken cancellationToken, GameModules owner)
            {
                m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                this.cancellationToken = cancellationToken;
            }

            /// <summary>
            ///   <para>创建已进入变更流程的句柄。</para>
            /// </summary>
            /// <param name="owner">所属容器。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            internal static ChangeScope CreateEntered(GameModules owner, CancellationToken cancellationToken) => new ChangeScope(cancellationToken, owner);

            /// <inheritdoc />
            public void Dispose() => m_Owner.EndChange();
        }

        /// <summary>
        ///   <para>Tick 暂停句柄；释放时恢复本层暂停。</para>
        /// </summary>
        internal readonly struct TickPauseScope : IDisposable
        {
            /// <summary>
            ///   <para>所属容器。</para>
            /// </summary>
            private readonly GameModules m_Owner;

            /// <summary>
            ///   <para>创建 Tick 暂停作用域。</para>
            /// </summary>
            /// <param name="owner">所属容器。</param>
            /// <param name="ct">取消令牌。</param>
            public TickPauseScope(GameModules owner, CancellationToken ct)
            {
                m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                owner.BeginTickPause(ct);
            }

            /// <inheritdoc />
            public void Dispose() => m_Owner.EndTickPause();
        }

        /// <summary>
        ///   <para>模块注册表。</para>
        /// </summary>
        private readonly GameModuleRegistry m_Registry = new();

        /// <summary>
        ///   <para>当前容器使用的 GameLoop 主线程 Tick 调度器。</para>
        /// </summary>
        private readonly IGameLoopTickSystemScheduler m_Scheduler;
        /// <summary>
        ///   <para>模块工厂。</para>
        /// </summary>
        private readonly IGameModuleFactory m_ModuleFactory;
        /// <summary>
        ///   <para>模块操作观察者。</para>
        /// </summary>
        internal IGameModuleObserver Observer { get; }

        /// <summary>
        ///   <para>模块工厂。</para>
        /// </summary>
        internal IGameModuleFactory ModuleFactory => m_ModuleFactory;

        /// <summary>
        ///   <para>模块拥有 Tick 对象的注册表。</para>
        /// </summary>
        private readonly GameModuleTickRegistry m_TickRegistry;

        /// <summary>
        ///   <para>模块生命周期执行器。</para>
        /// </summary>
        private readonly GameModuleLifecycleRunner m_LifecycleRunner;

        /// <summary>
        ///   <para>Tick 执行与容器释放之间的并发屏障。</para>
        /// </summary>
        private readonly object m_TickBarrierLock = new();

        /// <summary>
        ///   <para>当前正在执行中的 Tick 调用数量。</para>
        /// </summary>
        private int m_ActiveTickCount;

        /// <summary>
        ///   <para>当前是否有容器级模块变更正在进行。</para>
        /// </summary>
        private int m_IsChanging;

        /// <summary>
        ///   <para>当前是否因应用模块变更而暂停新 Tick 进入。</para>
        /// </summary>
        private int m_TickPauseDepth;

        /// <summary>
        ///   <para>容器释放状态机。</para>
        /// </summary>
        private int m_DisposeState;

        /// <summary>
        ///   <para>当前模块变更流程的取消源。</para>
        /// </summary>
        private CancellationTokenSource m_ChangeCancellation;

        /// <summary>
        ///   <para>当前模块变更流程结束通知。</para>
        /// </summary>
        private TaskCompletionSource<bool> m_ChangeCompletion;

        /// <summary>
        ///   <para>启动当前模块变更流程的线程编号。</para>
        /// </summary>
        private int m_ChangeOwnerThreadId;

        /// <summary>
        ///   <para>当前模块变更流程是否持有 Tick 暂停。</para>
        /// </summary>
        private bool m_ChangeOwnsTickPause;

        /// <summary>
        ///   <para>是否已释放。</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_DisposeState) == (int)DisposeState.Disposed;
        }

        /// <summary>
        ///   <para>是否正在释放。</para>
        /// </summary>
        public bool IsDisposing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_DisposeState) == (int)DisposeState.Disposing;
        }

        /// <summary>
        ///   <para>容器是否已不可再对外提供稳定访问。</para>
        /// </summary>
        internal bool IsDisposedOrDisposing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_DisposeState) != (int)DisposeState.Alive;
        }

        /// <summary>
        ///   <para>是否正在执行容器级模块变更。</para>
        /// </summary>
        public bool IsChanging
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_IsChanging) != 0;
        }

        /// <summary>
        ///   <para>当前所有已安装模块列表。</para>
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
        ///   <para>扩展交付记录；弱键不持有实例的强引用。</para>
        /// </summary>
        private static readonly ConditionalWeakTable<object, object> s_ClaimedExtensions = new();

        /// <summary>
        ///   <para>创建并接管扩展实例。</para>
        /// </summary>
        /// <param name="factory">扩展实例创建委托。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        private static T CreateOwnedExtension<T>(Func<T> factory) where T : class
        {
            var instance = factory() ?? throw new InvalidOperationException($"Factory returned null: {typeof(T).Name}.");
            lock (s_ClaimedExtensions)
            {
                if (s_ClaimedExtensions.TryGetValue(instance, out _))
                    throw new InvalidOperationException($"Extension instance was already transferred to a container: {typeof(T).Name}. Return a new instance for each container.");
                s_ClaimedExtensions.Add(instance, new object());
            }
            return instance;
        }

        /// <summary>
        ///   <para>创建模块容器。</para>
        /// </summary>
        /// <param name="options">容器创建选项。</param>
        public GameModules(GameModulesOptions options = null)
        {
            options ??= new GameModulesOptions();
            var createScheduler = options.CreateScheduler ?? throw new ArgumentNullException(nameof(options.CreateScheduler));
            var createModuleFactory = options.CreateModuleFactory ?? throw new ArgumentNullException(nameof(options.CreateModuleFactory));
            var createObserver = options.CreateObserver;
            m_ModuleFactory = CreateOwnedExtension(createModuleFactory);
            try
            {
                m_Scheduler = CreateOwnedExtension(createScheduler);
                if (createObserver != null) Observer = CreateOwnedExtension(createObserver);
                m_TickRegistry = new GameModuleTickRegistry(m_Registry, m_Scheduler);
                m_LifecycleRunner = new GameModuleLifecycleRunner(this, m_Registry);
            }
            catch (Exception failure)
            {
                ExceptionUtility.Rethrow(ExceptionUtility.Combine(failure, DisposeExtensions()));
                throw;
            }
        }

        /// <summary>
        ///   <para>将当前所有模块复制到指定列表中。</para>
        /// </summary>
        /// <param name="output">用于接收模块引用的列表。</param>
        public void CopyModulesTo(List<IGameModule> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            ThrowIfNotAlive();
            m_Registry.CopyModulesTo(output);
        }

        /// <summary>
        ///   <para>获取已安装模块。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public T GetModule<T>()
            where T : class, IGameModule
        {
            ThrowIfNotAlive();
            return m_Registry.GetModule<T>();
        }

        /// <summary>
        ///   <para>尝试获取已安装模块。</para>
        /// </summary>
        /// <param name="module">匹配到的模块实例。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public bool TryGetModule<T>(out T module)
            where T : class, IGameModule
        {
            if (IsDisposedOrDisposing)
            {
                module = null;
                return false;
            }

            if (m_Registry.TryGetModule(typeof(T), out var resolved) && resolved is T typedModule)
            {
                module = typedModule;
                return true;
            }

            module = null;
            return false;
        }

        /// <summary>
        ///   <para>供生命周期上下文在容器释放期间继续读取已声明依赖。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        internal T GetDependencyFromContext<T>(IGameModule ownerModule)
            where T : class, IGameModule => m_Registry.GetDependency(ownerModule, typeof(T)) as T;

        /// <summary>
        ///   <para>供生命周期上下文在容器释放期间继续尝试读取已声明依赖。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="module">模块。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        internal bool TryGetDependencyFromContext<T>(IGameModule ownerModule, out T module)
            where T : class, IGameModule
        {
            if (m_Registry.TryGetDependency(ownerModule, typeof(T), out var resolved) && resolved is T typedModule)
            {
                module = typedModule;
                return true;
            }

            module = null;
            return false;
        }

        /// <summary>
        ///   <para>使用容器的创建策略同步安装模块。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public void Install<T>()
            where T : GameModule => Install(typeof(T));

        /// <summary>
        ///   <para>安装模块。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        public void Install(Type moduleType)
        {
            var descriptor = new GameModuleDescriptor(moduleType);
            m_LifecycleRunner.Install(() => descriptor.CreateModule(m_ModuleFactory), moduleType);
        }

        /// <summary>
        ///   <para>使用容器的创建策略异步安装模块。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public ValueTask InstallAsync<T>()
            where T : GameModule => InstallAsync<T>(default);

        /// <summary>
        ///   <para>使用容器的创建策略异步安装模块。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public ValueTask InstallAsync<T>(CancellationToken ct)
            where T : GameModule => InstallAsync(typeof(T), ct);

        /// <summary>
        ///   <para>异步安装。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="ct">取消令牌。</param>
        public ValueTask InstallAsync(Type moduleType, CancellationToken ct = default)
        {
            var descriptor = new GameModuleDescriptor(moduleType);
            return m_LifecycleRunner.InstallAsync(() => descriptor.CreateModule(m_ModuleFactory), ct, moduleType);
        }

        /// <summary>
        ///   <para>使用模块创建工厂同步安装模块。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        public void Install(Func<GameModule> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            m_LifecycleRunner.Install(factory);
        }

        /// <summary>
        ///   <para>使用模块创建工厂异步安装模块。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        public ValueTask InstallAsync(Func<GameModule> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return InstallAsync(factory, default);
        }

        /// <summary>
        ///   <para>使用模块创建工厂异步安装模块。</para>
        /// </summary>
        /// <param name="factory">模块创建工厂。</param>
        /// <param name="ct">取消令牌。</param>
        public ValueTask InstallAsync(Func<GameModule> factory, CancellationToken ct)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return m_LifecycleRunner.InstallAsync(factory, ct);
        }
        
        /// <summary>
        ///   <para>按清单自身配置的安装顺序策略安装模块。</para>
        /// </summary>
        /// <param name="manifest">要执行的模块清单。</param>
        public void InstallFromManifest(GameModuleManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            m_LifecycleRunner.InstallFromManifest(manifest);
        }

        /// <summary>
        ///   <para>按清单自身配置的安装顺序策略异步安装模块。</para>
        /// </summary>
        /// <param name="manifest">要执行的模块清单。</param>
        public ValueTask InstallFromManifestAsync(GameModuleManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            return InstallFromManifestAsync(manifest, default);
        }

        /// <summary>
        ///   <para>按清单自身配置的安装顺序策略异步安装模块。</para>
        /// </summary>
        /// <param name="manifest">要执行的模块清单。</param>
        /// <param name="ct">取消令牌。</param>
        public ValueTask InstallFromManifestAsync(GameModuleManifest manifest, CancellationToken ct)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            return m_LifecycleRunner.InstallFromManifestAsync(manifest, ct);
        }

        /// <summary>
        ///   <para>卸载并释放指定精确类型的模块；回调失败也会完成资源清理。</para>
        /// </summary>
        /// <typeparam name="T">模块类型。</typeparam>
        public bool Uninstall<T>() where T : GameModule => m_LifecycleRunner.Uninstall(typeof(T));

        /// <summary>
        ///   <para>取消仅在开始拆除前生效；拆除开始后始终完成释放。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="T">模块类型。</typeparam>
        public ValueTask<bool> UninstallAsync<T>(CancellationToken ct = default) where T : GameModule => m_LifecycleRunner.UninstallAsync(typeof(T), ct);

        /// <summary>
        ///   <para>按逆安装顺序卸载并释放所有模块。</para>
        /// </summary>
        public void UninstallAll() => m_LifecycleRunner.UninstallAll();

        /// <summary>
        ///   <para>异步卸载全部。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        public ValueTask UninstallAllAsync(CancellationToken ct = default) => m_LifecycleRunner.UninstallAllAsync(ct);

        /// <summary>
        ///   <para>驱动当前容器的 Tick 调度。</para>
        /// </summary>
        /// <param name="deltaTime">当前 Tick 阶段的时间步长。</param>
        /// <param name="group">要执行的 Tick 分组。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tick(float deltaTime, TickGroup group)
        {
            if (!TryEnterTick()) return;

            try
            {
                using var tickScope = TickExecutionScope.Enter();
                m_Scheduler.Tick(deltaTime, group);
            }
            finally
            {
                ExitTick();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!PrepareDispose()) return;
            FinishDispose();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => DisposeAsync(default);

        /// <summary>
        ///   <para>异步释放模块容器。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        public async ValueTask DisposeAsync(CancellationToken ct)
        {
            if (!await PrepareDisposeAsync(ct)) return;
            await FinishDisposeAsync();
        }

        /// <summary>
        ///   <para>为指定模块创建生命周期上下文。</para>
        /// </summary>
        /// <param name="module">即将执行生命周期回调的模块实例。</param>
        /// <param name="deferTickRegistration">是否延迟登记 Tick 对象。</param>
        internal GameModuleContext CreateContext(IGameModule module, bool deferTickRegistration = false)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (IsDisposed) throw new ObjectDisposedException(nameof(GameModules));
            return new GameModuleContext(this, module, deferTickRegistration);
        }

        /// <summary>
        ///   <para>进入统一释放流程的公共前置阶段。</para>
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

            return true;
        }

        /// <summary>
        ///   <para>进入异步统一释放流程的公共前置阶段。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask<bool> PrepareDisposeAsync(CancellationToken ct)
        {
            if (!(await TryBeginDisposeAsync(ct))) return false;

            try
            {
                ct.ThrowIfCancellationRequested();
                m_Scheduler.WaitForIdle();
            }
            catch
            {
                ResetDisposeStateToAlive();
                throw;
            }

            return true;
        }

        /// <summary>
        ///   <para>执行同步释放收尾并聚合错误。</para>
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
                    ExceptionUtility.Add(ref errors, ex);
                }

                ExceptionUtility.Add(ref errors, DisposeExtensions());
            }
            finally
            {
                FinalizeDisposeState();
            }

            ThrowDisposeErrors(errors);
        }

        /// <summary>
        ///   <para>执行异步释放收尾并聚合错误。</para>
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
                    ExceptionUtility.Add(ref errors, ex);
                }

                ExceptionUtility.Add(ref errors, DisposeExtensions());
            }
            finally
            {
                FinalizeDisposeState();
            }

            ThrowDisposeErrors(errors);
        }

        /// <summary>
        ///   <para>释放扩展。</para>
        /// </summary>
        private Exception DisposeExtensions()
        {
            List<Exception> errors = null;
            try { m_Scheduler?.Dispose(); }
            catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            try { m_ModuleFactory.Dispose(); }
            catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            try { Observer?.Dispose(); }
            catch (Exception error) { ExceptionUtility.Add(ref errors, error); }
            return ExceptionUtility.Combine(errors);
        }

        /// <summary>
        ///   <para>通过生命周期上下文为模块注册拥有的 Tick 对象。</para>
        /// </summary>
        /// <param name="ownerModule">请求注册的模块所有者。</param>
        /// <param name="system">要注册的 Tick 对象。</param>
        internal void AddTickSystemFromContext(IGameModule ownerModule, object system)
        {
            ThrowIfNotAlive();
            m_TickRegistry.Add(ownerModule, system);
        }

        /// <summary>
        ///   <para>校验生命周期上下文请求注册的 Tick 对象。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="system">系统。</param>
        internal void ValidateTickSystemFromContext(IGameModule ownerModule, object system)
        {
            ThrowIfNotAlive();
            m_TickRegistry.ValidateAdd(ownerModule, system);
        }

        /// <summary>
        ///   <para>通过生命周期上下文移除模块注册的 Tick 对象。</para>
        /// </summary>
        /// <param name="ownerModule">请求移除的模块所有者。</param>
        /// <param name="system">要移除的 Tick 对象。</param>
        internal bool RemoveTickSystemFromContext(IGameModule ownerModule, object system)
        {
            if (IsDisposed) return false;
            return m_TickRegistry.Remove(ownerModule, system);
        }

        /// <summary>
        ///   <para>复制指定模块当前拥有的 Tick 对象登记，供运行时调试窗口读取快照。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        /// <param name="output">接收结果的集合。</param>
        internal void CopyOwnedTickRegistrationsTo(IGameModule ownerModule, List<GameModuleTickRegistration> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (ownerModule == null || IsDisposedOrDisposing) return;
            m_TickRegistry.CopyTo(ownerModule, output);
        }

        /// <summary>
        ///   <para>移除指定模块拥有的全部 Tick 对象。</para>
        /// </summary>
        /// <param name="ownerModule">所属模块。</param>
        internal void DetachOwnedTicks(IGameModule ownerModule) => m_TickRegistry.Detach(ownerModule);

        /// <summary>
        ///   <para>在容器已释放或正在释放时抛出异常。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfNotAlive()
        {
            if (IsDisposedOrDisposing) throw new ObjectDisposedException(nameof(GameModules));
        }

        /// <summary>
        ///   <para>开始一次容器释放流程，并等待所有当前 Tick 退出。</para>
        /// </summary>
        private bool TryBeginDispose()
        {
            while (true)
            {
                lock (m_TickBarrierLock)
                {
                    if (IsDisposed) return false;
                    if (TickExecutionScope.IsActive)
                    {
                        throw new InvalidOperationException($"Cannot dispose {nameof(GameModules)} from within Tick callbacks. Schedule the disposal after the current Tick.");
                    }

                    if (m_IsChanging != 0)
                    {
                        if (IsCurrentThreadOwningChangeNoLock() || GameModuleLifecycleScope.IsActive)
                        {
                            throw new InvalidOperationException($"Cannot dispose {nameof(GameModules)} from the same thread that is running a module lifecycle change.");
                        }

                        RequestCurrentChangeCancellationNoLock();
                        while (m_IsChanging != 0)
                        {
                            Monitor.Wait(m_TickBarrierLock, 50);
                        }

                        continue;
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
        }

        /// <summary>
        ///   <para>异步开始一次容器释放流程，并等待所有当前 Tick 退出。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask<bool> TryBeginDisposeAsync(CancellationToken ct)
        {
            while (true)
            {
                Task waitTask = null;
                lock (m_TickBarrierLock)
                {
                    ct.ThrowIfCancellationRequested();
                    if (IsDisposed) return false;
                    if (TickExecutionScope.IsActive)
                    {
                        throw new InvalidOperationException($"Cannot dispose {nameof(GameModules)} from within Tick callbacks. Schedule the disposal after the current Tick.");
                    }

                    if (m_IsChanging != 0)
                    {
                        if (GameModuleLifecycleScope.IsActive)
                        {
                            throw new InvalidOperationException($"Cannot dispose {nameof(GameModules)} from within a module lifecycle callback.");
                        }

                        RequestCurrentChangeCancellationNoLock();
                        waitTask = m_ChangeCompletion?.Task;
                    }
                    else
                    {
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

                        return true;
                    }
                }

                await WaitForChangeCompletionAsync(waitTask, ct);
            }
        }

        /// <summary>
        ///   <para>把容器释放状态回滚到可运行状态。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetDisposeStateToAlive() => Volatile.Write(ref m_DisposeState, (int)DisposeState.Alive);

        /// <summary>
        ///   <para>完成容器释放状态并通知外部句柄层。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FinalizeDisposeState()
        {
            Volatile.Write(ref m_DisposeState, (int)DisposeState.Disposed);
            Game.NotifyModulesDisposed(this);
        }

        /// <summary>
        ///   <para>登记某个模块实例的容器所有权。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal void TakeModuleOwnership(IGameModule module) => ((GameModule)module).TakeOwnership(this);

        /// <summary>
        ///   <para>释放某个模块实例的容器所有权。</para>
        /// </summary>
        /// <param name="module">模块。</param>
        internal void ReleaseModuleOwnership(IGameModule module) => ((GameModule)module).ReleaseOwnership(this);

        /// <summary>
        ///   <para>进入一次容器级模块变更流程。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        internal ChangeScope EnterChangeScope(CancellationToken ct = default) => new ChangeScope(this, ct);

        /// <summary>
        ///   <para>异步进入一次不暂停 Tick 的容器级模块变更流程。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        internal async ValueTask<ChangeScope> EnterChangeOperationScopeAsync(CancellationToken ct = default)
        {
            var token = await BeginChangeAsync(ct, pauseTicks: false);
            return ChangeScope.CreateEntered(this, token);
        }

        /// <summary>
        ///   <para>短暂暂停 Tick，用于应用注册表或调度器变更。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        internal TickPauseScope EnterTickPauseScope(CancellationToken ct = default) => new TickPauseScope(this, ct);

        /// <summary>
        ///   <para>开始一次容器级模块变更。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <param name="pauseTicks">是否暂停 Tick。</param>
        private CancellationToken BeginChange(CancellationToken ct, bool pauseTicks)
        {
            while (true)
            {
                lock (m_TickBarrierLock)
                {
                    ct.ThrowIfCancellationRequested();
                    EnsureCanStartOrWaitForChangeNoLock();

                    if (m_IsChanging == 0)
                    {
                        return StartChangeNoLock(ct, pauseTicks);
                    }

                    if (IsCurrentThreadOwningChangeNoLock())
                    {
                        throw new InvalidOperationException("Cannot wait for a module change started on the same thread. Await the current change first.");
                    }

                    while (m_IsChanging != 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        Monitor.Wait(m_TickBarrierLock, 50);
                    }
                }
            }
        }

        /// <summary>
        ///   <para>异步开始一次容器级模块变更。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <param name="pauseTicks">是否暂停 Tick。</param>
        private async ValueTask<CancellationToken> BeginChangeAsync(CancellationToken ct, bool pauseTicks)
        {
            while (true)
            {
                Task waitTask = null;
                lock (m_TickBarrierLock)
                {
                    ct.ThrowIfCancellationRequested();
                    EnsureCanStartOrWaitForChangeNoLock();

                    if (m_IsChanging == 0)
                    {
                        return StartChangeNoLock(ct, pauseTicks);
                    }

                    waitTask = m_ChangeCompletion?.Task;
                }

                await WaitForChangeCompletionAsync(waitTask, ct);
            }
        }

        /// <summary>
        ///   <para>校验当前调用是否可以进入或等待模块变更。</para>
        /// </summary>
        private void EnsureCanStartOrWaitForChangeNoLock()
        {
            if (IsDisposedOrDisposing) throw new ObjectDisposedException(nameof(GameModules));
            if (TickExecutionScope.IsActive)
            {
                throw new InvalidOperationException("Cannot change modules from within Tick callbacks. Schedule the change after the current Tick.");
            }

            if (GameModuleLifecycleScope.IsActive)
            {
                throw new InvalidOperationException("Cannot start a nested module change from within a module lifecycle callback.");
            }
        }

        /// <summary>
        ///   <para>进入模块变更状态，并按需暂停新 Tick 进入。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <param name="pauseTicks">是否暂停 Tick。</param>
        private CancellationToken StartChangeNoLock(CancellationToken ct, bool pauseTicks)
        {
            m_IsChanging = 1;
            m_ChangeOwnerThreadId = Thread.CurrentThread.ManagedThreadId;
            m_ChangeCancellation = ct.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : new CancellationTokenSource();
            m_ChangeCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (pauseTicks)
            {
                m_ChangeOwnsTickPause = true;
                BeginTickPauseNoLock();
            }

            if (IsDisposedOrDisposing)
            {
                ClearChangeStateNoLock(out var cancellation, out var completion);
                Monitor.PulseAll(m_TickBarrierLock);
                CompleteChangeWaiters(cancellation, completion);
                throw new ObjectDisposedException(nameof(GameModules));
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

            return m_ChangeCancellation.Token;
        }

        /// <summary>
        ///   <para>开始一次短暂 Tick 暂停。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private void BeginTickPause(CancellationToken ct)
        {
            lock (m_TickBarrierLock)
            {
                ct.ThrowIfCancellationRequested();
                if (IsDisposedOrDisposing) throw new ObjectDisposedException(nameof(GameModules));
                if (TickExecutionScope.IsActive)
                {
                    throw new InvalidOperationException("Cannot pause module ticks from within Tick callbacks. Schedule the change after the current Tick.");
                }

                BeginTickPauseNoLock();
            }

            try
            {
                m_Scheduler.WaitForIdle();
            }
            catch
            {
                EndTickPause();
                throw;
            }
        }

        /// <summary>
        ///   <para>进入 Tick 暂停状态，并等待当前 Tick 退出。</para>
        /// </summary>
        private void BeginTickPauseNoLock()
        {
            m_TickPauseDepth++;
            while (m_ActiveTickCount > 0)
            {
                Monitor.Wait(m_TickBarrierLock);
            }
        }

        /// <summary>
        ///   <para>结束一次短暂 Tick 暂停。</para>
        /// </summary>
        private void EndTickPause()
        {
            lock (m_TickBarrierLock)
            {
                if (m_TickPauseDepth <= 0) return;
                m_TickPauseDepth--;
                if (m_TickPauseDepth == 0)
                {
                    Monitor.PulseAll(m_TickBarrierLock);
                }
            }
        }

        /// <summary>
        ///   <para>结束一次容器级模块变更并恢复 Tick 进入。</para>
        /// </summary>
        private void EndChange()
        {
            CancellationTokenSource cancellation = null;
            TaskCompletionSource<bool> completion = null;

            lock (m_TickBarrierLock)
            {
                if (m_IsChanging == 0) return;
                ClearChangeStateNoLock(out cancellation, out completion);
                Monitor.PulseAll(m_TickBarrierLock);
            }

            CompleteChangeWaiters(cancellation, completion);
        }

        /// <summary>
        ///   <para>清理当前变更状态。</para>
        /// </summary>
        /// <param name="cancellation">取消源。</param>
        /// <param name="completion">完成通知。</param>
        private void ClearChangeStateNoLock(
            out CancellationTokenSource cancellation,
            out TaskCompletionSource<bool> completion)
        {
            cancellation = m_ChangeCancellation;
            completion = m_ChangeCompletion;
            m_ChangeCancellation = null;
            m_ChangeCompletion = null;
            m_ChangeOwnerThreadId = 0;
            if (m_ChangeOwnsTickPause)
            {
                m_ChangeOwnsTickPause = false;
                if (m_TickPauseDepth > 0)
                {
                    m_TickPauseDepth--;
                }
            }

            m_IsChanging = 0;
        }

        /// <summary>
        ///   <para>通知等待变更结束的同步/异步调用方。</para>
        /// </summary>
        /// <param name="cancellation">取消源。</param>
        /// <param name="completion">完成通知。</param>
        private static void CompleteChangeWaiters(
            CancellationTokenSource cancellation,
            TaskCompletionSource<bool> completion)
        {
            cancellation?.Dispose();
            completion?.TrySetResult(true);
        }

        /// <summary>
        ///   <para>请求当前模块变更尽快取消。</para>
        /// </summary>
        private void RequestCurrentChangeCancellationNoLock()
        {
            try
            {
                m_ChangeCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 生命周期结束时重复取消是允许的并发竞态。
            }
        }

        /// <summary>
        ///   <para>当前线程是否就是启动模块变更的线程。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsCurrentThreadOwningChangeNoLock()
        {
            return m_ChangeOwnerThreadId != 0 &&
                   m_ChangeOwnerThreadId == Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        ///   <para>异步等待变更完成通知。</para>
        /// </summary>
        /// <param name="waitTask">等待任务。</param>
        /// <param name="ct">取消令牌。</param>
        private static async ValueTask WaitForChangeCompletionAsync(Task waitTask, CancellationToken ct)
        {
            if (waitTask == null)
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                return;
            }

            if (!ct.CanBeCanceled)
            {
                await waitTask;
                return;
            }

            var cancelTask = Task.Delay(Timeout.Infinite, ct);
            if (await Task.WhenAny(waitTask, cancelTask) == cancelTask)
            {
                ct.ThrowIfCancellationRequested();
            }

            await waitTask;
        }

        /// <summary>
        ///   <para>返回对象支持的 Tick 相位文本。</para>
        /// </summary>
        /// <param name="system">系统。</param>
        internal static string GetTickPhasesText(object system) => GameModuleTickRegistry.GetTickPhasesText(system);

        /// <summary>
        ///   <para>尝试进入一次 Tick 执行。</para>
        /// </summary>
        private bool TryEnterTick()
        {
            lock (m_TickBarrierLock)
            {
                if (IsDisposedOrDisposing || m_TickPauseDepth != 0) return false;
                m_ActiveTickCount++;
                return true;
            }
        }

        /// <summary>
        ///   <para>结束一次 Tick 执行并在空闲时唤醒等待释放的线程。</para>
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
        ///   <para>把容器释放期间收集到的异常统一抛出。</para>
        /// </summary>
        /// <param name="errors">错误。</param>
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