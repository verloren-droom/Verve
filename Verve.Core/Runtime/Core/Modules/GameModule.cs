namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>模块标记接口。</para>
    /// </summary>
    public interface IGameModule { }
    
    /// <summary>
    ///   <para>模块基类。</para>
    /// </summary>
    [Serializable]
    public abstract class GameModule : IGameModule, IDisposable
    {
        /// <summary>
        ///   <para>模块生命周期状态。</para>
        /// </summary>
        private enum ModuleState : byte
        {
            /// <summary>
            ///   <para>模块尚未安装到容器中。</para>
            /// </summary>
            Uninstalled = 0,
            /// <summary>
            ///   <para>模块已完成安装并处于可运行状态。</para>
            /// </summary>
            Installed = 1,
            /// <summary>
            ///   <para>模块正在执行安装回调。</para>
            /// </summary>
            Installing = 2,
            /// <summary>
            ///   <para>模块正在执行卸载回调。</para>
            /// </summary>
            Uninstalling = 3,
            /// <summary>
            ///   <para>模块实例已经释放，不允许再次进入生命周期。</para>
            /// </summary>
            Disposed = 4,
        }

        /// <summary>
        ///   <para>模块当前状态。</para>
        /// </summary>
        [NonSerialized] private int m_State;
        /// <summary>
        ///   <para>所属容器。</para>
        /// </summary>
        [NonSerialized] private GameModules m_Owner;
        /// <summary>
        ///   <para>是否已被容器接管。</para>
        /// </summary>
        internal bool HasOwner => Volatile.Read(ref m_Owner) != null;

        /// <summary>
        ///   <para>接管模块所有权。</para>
        /// </summary>
        /// <param name="owner">所属容器。</param>
        internal void TakeOwnership(GameModules owner)
        {
            if (Interlocked.CompareExchange(ref m_Owner, owner, null) != null)
                throw new InvalidOperationException($"Module {GetType().FullName} already belongs to a container.");
        }

        /// <summary>
        ///   <para>解除模块所有权登记。</para>
        /// </summary>
        /// <param name="owner">所属容器。</param>
        internal void ReleaseOwnership(GameModules owner) => Interlocked.CompareExchange(ref m_Owner, null, owner);

        /// <summary>
        ///   <para>是否已安装。</para>
        /// </summary>
        public bool IsInstalled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_State) == (int)ModuleState.Installed;
        }

        /// <summary>
        ///   <para>模块实例是否已经释放。</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_State) == (int)ModuleState.Disposed;
        }

        /// <summary>
        ///   <para>子类实现安装逻辑。</para>
        /// </summary>
        /// <param name="context">当前模块的生命周期上下文。</param>
        /// <param name="ct">生命周期取消令牌。</param>
        protected virtual ValueTask OnInstall(GameModuleContext context, CancellationToken ct) => default;

        /// <summary>
        ///   <para>子类实现卸载逻辑。</para>
        /// </summary>
        /// <param name="context">当前模块的生命周期上下文。</param>
        /// <param name="ct">生命周期取消令牌。</param>
        protected virtual ValueTask OnUninstall(GameModuleContext context, CancellationToken ct) => default;

        /// <summary>
        ///   <para>子类实现释放逻辑。</para>
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        ///   <para>返回当前生命周期状态文本，供调试输出使用。</para>
        /// </summary>
        internal string GetStateText() => ((ModuleState)Volatile.Read(ref m_State)).ToString();

        /// <summary>
        ///   <para>异步安装。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        internal ValueTask InstallAsync(GameModuleContext context, CancellationToken ct)
        {
            return ExecuteAsyncTransition(
                context,
                ct,
                ModuleState.Uninstalled,
                ModuleState.Installing,
                ModuleState.Installed,
                ModuleState.Uninstalled,
                install: true);
        }
        
        /// <summary>
        ///   <para>异步卸载。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        internal ValueTask UninstallAsync(GameModuleContext context, CancellationToken ct)
        {
            return ExecuteAsyncTransition(
                context,
                ct,
                ModuleState.Installed,
                ModuleState.Uninstalling,
                ModuleState.Uninstalled,
                ModuleState.Installed,
                install: false);
        }

        /// <summary>
        ///   <para>同步安装。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        internal void InstallSync(GameModuleContext context, CancellationToken ct)
        {
            ExecuteSyncTransition(
                context,
                ct,
                ModuleState.Uninstalled,
                ModuleState.Installing,
                ModuleState.Installed,
                ModuleState.Uninstalled,
                install: true);
        }

        /// <summary>
        ///   <para>同步卸载。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        internal void UninstallSync(GameModuleContext context, CancellationToken ct)
        {
            ExecuteSyncTransition(
                context,
                ct,
                ModuleState.Installed,
                ModuleState.Uninstalling,
                ModuleState.Uninstalled,
                ModuleState.Installed,
                install: false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Volatile.Read(ref m_Owner) != null)
                throw new InvalidOperationException($"Module {GetType().FullName} is container-owned. Uninstall it through its owner.");
            DisposeCore(owned: false);
        }

        /// <summary>
        ///   <para>释放容器拥有的模块。</para>
        /// </summary>
        /// <param name="owner">所属容器。</param>
        internal void DisposeOwned(GameModules owner)
        {
            if (!ReferenceEquals(Volatile.Read(ref m_Owner), owner))
                throw new InvalidOperationException($"Only the owning container can release module {GetType().FullName}.");
            DisposeCore(owned: true);
        }

        /// <summary>
        ///   <para>执行资源释放。</para>
        /// </summary>
        /// <param name="owned">是否由容器拥有。</param>
        private void DisposeCore(bool owned)
        {
            while (true)
            {
                int state = Volatile.Read(ref m_State);
                if (state == (int)ModuleState.Disposed) return;
                // 卸载回调失败也必须释放资源；运行中的生命周期不能被提前销毁。
                if (state != (int)ModuleState.Uninstalled && !(owned && state == (int)ModuleState.Installed))
                    throw new InvalidOperationException($"Cannot dispose module {GetType().FullName} while it is {(ModuleState)state}.");
                if (Interlocked.CompareExchange(ref m_State, (int)ModuleState.Disposed, state) == state) break;
            }
            OnDispose();
        }
        
        /// <summary>
        ///   <para>异步执行一次安装或卸载状态转换。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="fromState">转换前状态。</param>
        /// <param name="transitionState">过渡状态。</param>
        /// <param name="successState">成功状态。</param>
        /// <param name="failState">失败状态。</param>
        /// <param name="install">是否执行安装。</param>
        private ValueTask ExecuteAsyncTransition(
            GameModuleContext context,
            CancellationToken ct,
            ModuleState fromState,
            ModuleState transitionState,
            ModuleState successState,
            ModuleState failState,
            bool install)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            ct.ThrowIfCancellationRequested();
            BeginTransition(fromState, transitionState);

            try
            {
                var operationTask = InvokeLifecycle(context, ct, install);
                if (operationTask.IsCompleted)
                {
                    operationTask.GetAwaiter().GetResult();
                    ct.ThrowIfCancellationRequested();
                    CompleteTransition(success: true, successState, failState);
                    return default;
                }

                return AwaitTransitionCompletion(operationTask, successState, failState, ct);
            }
            catch
            {
                CompleteTransition(success: false, successState, failState);
                throw;
            }
        }

        /// <summary>
        ///   <para>同步执行一次安装或卸载状态转换。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="fromState">转换前状态。</param>
        /// <param name="transitionState">过渡状态。</param>
        /// <param name="successState">成功状态。</param>
        /// <param name="failState">失败状态。</param>
        /// <param name="install">是否执行安装。</param>
        private void ExecuteSyncTransition(
            GameModuleContext context,
            CancellationToken ct,
            ModuleState fromState,
            ModuleState transitionState,
            ModuleState successState,
            ModuleState failState,
            bool install)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            ct.ThrowIfCancellationRequested();
            BeginTransition(fromState, transitionState);

            bool success = false;
            try
            {
                var operationTask = InvokeLifecycle(context, ct, install);
                if (!operationTask.IsCompleted)
                {
                    throw CreateIncompleteSyncLifecycleException(install);
                }

                operationTask.GetAwaiter().GetResult();
                ct.ThrowIfCancellationRequested();
                success = true;
            }
            finally
            {
                CompleteTransition(success, successState, failState);
            }
        }

        /// <summary>
        ///   <para>调用实际的模块生命周期回调。</para>
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="install">是否执行安装。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask InvokeLifecycle(GameModuleContext context, CancellationToken ct, bool install) => install ? OnInstall(context, ct) : OnUninstall(context, ct);

        /// <summary>
        ///   <para>等待异步状态转换结束并设置最终状态。</para>
        /// </summary>
        /// <param name="operationTask">操作任务。</param>
        /// <param name="successState">成功状态。</param>
        /// <param name="failState">失败状态。</param>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask AwaitTransitionCompletion(
            ValueTask operationTask,
            ModuleState successState,
            ModuleState failState,
            CancellationToken ct)
        {
            bool success = false;
            try
            {
                await operationTask;
                ct.ThrowIfCancellationRequested();
                success = true;
            }
            finally
            {
                CompleteTransition(success, successState, failState);
            }
        }

        /// <summary>
        ///   <para>开始状态转换；若当前状态不匹配则抛出异常。</para>
        /// </summary>
        /// <param name="fromState">转换前状态。</param>
        /// <param name="toState">目标状态。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginTransition(ModuleState fromState, ModuleState toState)
        {
            int observedState = Interlocked.CompareExchange(ref m_State, (int)toState, (int)fromState);
            if (observedState == (int)fromState) return;
            throw new InvalidOperationException(
                $"Cannot transition module {GetType().FullName} from {fromState} to {toState} while it is {(ModuleState)observedState}.");
        }

        /// <summary>
        ///   <para>完成状态转换。</para>
        /// </summary>
        /// <param name="success">是否成功。</param>
        /// <param name="successState">成功状态。</param>
        /// <param name="failState">失败状态。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CompleteTransition(bool success, ModuleState successState, ModuleState failState) => Volatile.Write(ref m_State, success ? (int)successState : (int)failState);

        /// <summary>
        ///   <para>同步入口收到未完成的生命周期操作时抛出明确异常。</para>
        /// </summary>
        /// <param name="install">是否执行安装。</param>
        private InvalidOperationException CreateIncompleteSyncLifecycleException(bool install)
        {
            string operation = install ? "install" : "uninstall";
            string guidance = install
                ? $"Use {nameof(GameModules)}.{nameof(GameModules.InstallAsync)} instead."
                : $"Use {nameof(GameModules)}.{nameof(GameModules.UninstallAsync)} or {nameof(GameModules)}.{nameof(GameModules.DisposeAsync)} instead.";

            return new InvalidOperationException(
                $"Module {GetType().FullName} returned an incomplete {(operation)} operation during synchronous execution. {guidance}");
        }
    }
}