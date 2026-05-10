namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    /// <summary>
    ///   <para>模块标记接口</para>
    /// </summary>
    public interface IGameModule { }

    /// <summary>
    ///   <para>模块依赖声明接口</para>
    /// </summary>
    public interface IGameModuleDependencies
    {
        /// <summary>
        ///   <para>依赖模块列表</para>
        /// </summary>
        IReadOnlyList<Type> Dependencies { get; }
    }

    /// <summary>
    ///   <para>模块基类</para>
    /// </summary>
    [Serializable]
    public abstract class GameModule : IGameModule, IDisposable
    {
        /// <summary>
        ///   <para>模块生命周期状态</para>
        /// </summary>
        private enum ModuleState : byte
        {
            /// <summary>
            ///   <para>模块尚未安装到容器中</para>
            /// </summary>
            Uninstalled = 0,
            /// <summary>
            ///   <para>模块已完成安装并处于可运行状态</para>
            /// </summary>
            Installed = 1,
            /// <summary>
            ///   <para>模块正在执行安装回调</para>
            /// </summary>
            Installing = 2,
            /// <summary>
            ///   <para>模块正在执行卸载回调</para>
            /// </summary>
            Uninstalling = 3,
            /// <summary>
            ///   <para>模块实例已经释放，不允许再次进入生命周期</para>
            /// </summary>
            Disposed = 4,
        }

        /// <summary>
        ///   <para>模块当前状态</para>
        /// </summary>
        [NonSerialized] private int m_State;

        /// <summary>
        ///   <para>是否已安装</para>
        /// </summary>
        public bool IsInstalled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_State) == (int)ModuleState.Installed;
        }

        /// <summary>
        ///   <para>模块实例是否已经释放</para>
        /// </summary>
        public bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref m_State) == (int)ModuleState.Disposed;
        }

        /// <summary>
        ///   <para>子类实现安装逻辑</para>
        /// </summary>
        /// <param name="context">当前模块的生命周期上下文</param>
        protected virtual ValueTask OnInstall(GameModuleContext context) => default;

        /// <summary>
        ///   <para>子类实现卸载逻辑</para>
        /// </summary>
        /// <param name="context">当前模块的生命周期上下文</param>
        protected virtual ValueTask OnUninstall(GameModuleContext context) => default;

        /// <summary>
        ///   <para>子类实现释放逻辑</para>
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        ///   <para>返回当前生命周期状态文本，供调试输出使用</para>
        /// </summary>
        internal string GetStateText()
        {
            return ((ModuleState)Volatile.Read(ref m_State)).ToString();
        }

        /// <summary>
        ///   <para>异步安装</para>
        /// </summary>
        internal ValueTask InstallAsync(GameModuleContext context)
        {
            return ExecuteAsyncTransition(
                context,
                ModuleState.Uninstalled,
                ModuleState.Installing,
                ModuleState.Installed,
                ModuleState.Uninstalled,
                install: true);
        }
        
        /// <summary>
        ///   <para>异步卸载</para>
        /// </summary>
        internal ValueTask UninstallAsync(GameModuleContext context)
        {
            return ExecuteAsyncTransition(
                context,
                ModuleState.Installed,
                ModuleState.Uninstalling,
                ModuleState.Uninstalled,
                ModuleState.Installed,
                install: false);
        }

        /// <summary>
        ///   <para>同步安装</para>
        /// </summary>
        internal void InstallSync(GameModuleContext context)
        {
            ExecuteSyncTransition(
                context,
                ModuleState.Uninstalled,
                ModuleState.Installing,
                ModuleState.Installed,
                ModuleState.Uninstalled,
                install: true);
        }

        /// <summary>
        ///   <para>同步卸载</para>
        /// </summary>
        internal void UninstallSync(GameModuleContext context)
        {
            ExecuteSyncTransition(
                context,
                ModuleState.Installed,
                ModuleState.Uninstalling,
                ModuleState.Uninstalled,
                ModuleState.Installed,
                install: false);
        }

        /// <summary>
        ///   <para>释放模块持有的资源</para>
        /// </summary>
        public void Dispose()
        {
            int observedState = Interlocked.CompareExchange(
                ref m_State,
                (int)ModuleState.Disposed,
                (int)ModuleState.Uninstalled);
            if (observedState == (int)ModuleState.Disposed)
            {
                return;
            }

            if (observedState != (int)ModuleState.Uninstalled)
            {
                throw new InvalidOperationException(
                    $"Cannot dispose module {GetType().FullName} while it is {(ModuleState)observedState}. Uninstall it from {nameof(GameModules)} first.");
            }

            OnDispose();
        }
        
        /// <summary>
        ///   <para>异步执行一次安装或卸载状态转换</para>
        /// </summary>
        private ValueTask ExecuteAsyncTransition(
            GameModuleContext context,
            ModuleState fromState,
            ModuleState transitionState,
            ModuleState successState,
            ModuleState failState,
            bool install)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            BeginTransition(fromState, transitionState);

            try
            {
                var operationTask = InvokeLifecycle(context, install);
                if (operationTask.IsCompleted)
                {
                    operationTask.GetAwaiter().GetResult();
                    CompleteTransition(success: true, successState, failState);
                    return default;
                }

                return AwaitTransitionCompletion(operationTask, successState, failState);
            }
            catch
            {
                CompleteTransition(success: false, successState, failState);
                throw;
            }
        }

        /// <summary>
        ///   <para>同步执行一次安装或卸载状态转换</para>
        /// </summary>
        private void ExecuteSyncTransition(
            GameModuleContext context,
            ModuleState fromState,
            ModuleState transitionState,
            ModuleState successState,
            ModuleState failState,
            bool install)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            BeginTransition(fromState, transitionState);

            bool success = false;
            try
            {
                var operationTask = InvokeLifecycle(context, install);
                if (!operationTask.IsCompleted)
                {
                    throw CreateIncompleteSyncLifecycleException(install);
                }

                operationTask.GetAwaiter().GetResult();
                success = true;
            }
            finally
            {
                CompleteTransition(success, successState, failState);
            }
        }

        /// <summary>
        ///   <para>调用实际的模块生命周期回调</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask InvokeLifecycle(GameModuleContext context, bool install)
        {
            return install ? OnInstall(context) : OnUninstall(context);
        }

        /// <summary>
        ///   <para>等待异步状态转换结束并提交最终状态</para>
        /// </summary>
        private async ValueTask AwaitTransitionCompletion(ValueTask operationTask, ModuleState successState, ModuleState failState)
        {
            bool success = false;
            try
            {
                await operationTask;
                success = true;
            }
            finally
            {
                CompleteTransition(success, successState, failState);
            }
        }

        /// <summary>
        ///   <para>开始状态转换；若当前状态不匹配则抛出异常</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginTransition(ModuleState fromState, ModuleState toState)
        {
            int observedState = Interlocked.CompareExchange(ref m_State, (int)toState, (int)fromState);
            if (observedState == (int)fromState) return;
            throw new InvalidOperationException(
                $"Cannot transition module {GetType().FullName} from {fromState} to {toState} while it is {(ModuleState)observedState}.");
        }

        /// <summary>
        ///   <para>完成状态转换</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CompleteTransition(bool success, ModuleState successState, ModuleState failState)
        {
            Volatile.Write(ref m_State, success ? (int)successState : (int)failState);
        }

        /// <summary>
        ///   <para>同步入口收到未完成的生命周期操作时抛出明确异常</para>
        /// </summary>
        private InvalidOperationException CreateIncompleteSyncLifecycleException(bool install)
        {
            string operation = install ? "install" : "uninstall";
            string guidance = install
                ? $"Use {nameof(GameModules)}.{nameof(GameModules.InstallAsync)} instead."
                : $"Use {nameof(GameModules)}.{nameof(GameModules.UninstallAsync)} or {nameof(GameModules)}.{nameof(GameModules.DisposeAsync)} instead.";

            return new InvalidOperationException(
                $"Module {GetType().FullName} returned an incomplete {operation} operation during synchronous execution. {guidance}");
        }
    }
}