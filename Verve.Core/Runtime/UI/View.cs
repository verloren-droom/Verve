namespace Verve
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    ///   <para><see cref="ViewBase"/> 的生命周期状态。</para>
    /// </summary>
    internal enum ViewState : byte
    {
        /// <summary>
        ///   <para>未绑定。</para>
        /// </summary>
        Detached,
        /// <summary>
        ///   <para>已绑定。</para>
        /// </summary>
        Attached,
        /// <summary>
        ///   <para>已创建。</para>
        /// </summary>
        Created,
        /// <summary>
        ///   <para>已打开。</para>
        /// </summary>
        Opened,
        /// <summary>
        ///   <para>已关闭。</para>
        /// </summary>
        Closed,
        /// <summary>
        ///   <para>已释放。</para>
        /// </summary>
        Released,
    }

    /// <summary>
    ///   <para>页面基类；由 UI 模块创建和释放。</para>
    /// </summary>
    public abstract class ViewBase
    {
        /// <summary>
        ///   <para>组件。</para>
        /// </summary>
        private UIViewComponent m_Component;
        /// <summary>
        ///   <para>打开参数；关闭后清除借用引用。</para>
        /// </summary>
        private ViewArgs m_OpenArgs;
        /// <summary>
        ///   <para>状态。</para>
        /// </summary>
        private ViewState m_State;
        /// <summary>
        ///   <para>是否打开中。</para>
        /// </summary>
        private bool m_IsOpening;
        /// <summary>
        ///   <para>是否关闭中。</para>
        /// </summary>
        private bool m_IsClosing;
        /// <summary>
        ///   <para>是否释放中。</para>
        /// </summary>
        private bool m_IsReleasing;
        /// <summary>
        ///   <para>事件已绑定。</para>
        /// </summary>
        private bool m_EventsBound;
        /// <summary>
        ///   <para>已请求释放。</para>
        /// </summary>
        private bool m_ReleaseRequested;
        /// <summary>
        ///   <para>已请求关闭。</para>
        /// </summary>
        private bool m_CloseRequested;
        /// <summary>
        ///   <para>页面名称。</para>
        /// </summary>
        private string m_ViewName;
        /// <summary>
        ///   <para>页面局部组件。</para>
        /// </summary>
        private List<ViewPartBase> m_ViewParts;
        /// <summary>
        ///   <para>拥有的部件。</para>
        /// </summary>
        private List<WidgetBase> m_OwnedWidgets;
        /// <summary>
        ///   <para>是否正在清理局部组件。</para>
        /// </summary>
        private bool m_IsPruningViewParts;
        /// <summary>
        ///   <para>异步操作锁。</para>
        /// </summary>
        private readonly object m_AsyncOperationLock = new();
        /// <summary>
        ///   <para>最后一次异步操作。</para>
        /// </summary>
        private Task m_LastAsyncOperation = Task.CompletedTask;
        /// <summary>
        ///   <para>进行中的异步操作数量。</para>
        /// </summary>
        private int m_AsyncOperationCount;
        /// <summary>
        ///   <para>当前异步操作作用域。</para>
        /// </summary>
        private static readonly AsyncLocal<AsyncOperationScope> s_AsyncOperationScope = new();

        /// <summary>
        ///   <para>创建页面基类。</para>
        /// </summary>
        protected ViewBase()
        {
            if (!UIInstanceCreator.IsCreating(typeof(ViewBase)))
            {
                throw new InvalidOperationException(
                    $"View 类型 {GetType().FullName} 只能由 {nameof(UIModule)} 创建（仅限程序集内部工厂），不能外部创建。");
            }
        }

        /// <summary>
        ///   <para>返回页面 Prefab 上的 UI 组件。</para>
        /// </summary>
        public UIViewComponent Component => m_Component;

        /// <summary>
        ///   <para>返回由代码生成器写入的变量签名。</para>
        /// </summary>
        protected virtual int VariableSignature => 0;

        /// <summary>
        ///   <para>返回页面当前是否打开。</para>
        /// </summary>
        public bool IsOpen => m_State == ViewState.Opened && m_Component != null;

        /// <summary>
        ///   <para>返回页面是否已经释放或其根组件已被 Unity 销毁。</para>
        /// </summary>
        public bool IsReleased => m_State == ViewState.Released || m_Component == null;

        /// <summary>
        ///   <para>返回当前 <see cref="ViewBase"/> 创建的局部组件，仅供运行时诊断读取。</para>
        /// </summary>
        internal IReadOnlyList<ViewPartBase> ViewParts
        {
            get
            {
                PruneReleasedViewParts();
                return m_ViewParts;
            }
        }

        /// <summary>
        ///   <para>返回当前 <see cref="ViewBase"/> 直接持有的 <see cref="WidgetBase"/>，仅供运行时诊断读取。</para>
        /// </summary>
        internal IReadOnlyList<WidgetBase> Widgets => m_OwnedWidgets;

        /// <summary>
        ///   <para>打开参数；供模块检查复用请求和泛型页面读取。</para>
        /// </summary>
        internal ViewArgs OpenArgsInternal => m_OpenArgs;

        /// <summary>
        ///   <para>返回用于日志和诊断的页面名称，默认去除 <see cref="ViewBase"/> 后缀。</para>
        /// </summary>
        public string ViewName
        {
            get
            {
                if (m_ViewName != null)
                {
                    return m_ViewName;
                }

                var typeName = GetType().Name;
                m_ViewName = typeName.EndsWith("View", StringComparison.Ordinal)
                    ? typeName[..^4]
                    : typeName;
                return m_ViewName;
            }
        }

        /// <summary>
        ///   <para>发布 <see cref="ViewBase"/> 生命周期状态通知，仅供 UI 模块和可选绑定逻辑订阅。</para>
        /// </summary>
        internal event Action<ViewBase, ViewState> StateChanged;

        /// <summary>
        ///   <para>返回 <see cref="ViewBase"/> 是否正在执行打开回调。</para>
        /// </summary>
        internal bool IsOpening => m_IsOpening;

        /// <summary>
        ///   <para>返回 <see cref="ViewBase"/> 是否正在执行关闭回调。</para>
        /// </summary>
        internal bool IsClosing => m_IsClosing;

        /// <summary>
        ///   <para>返回当前是否存在等待或执行中的异步页面操作。</para>
        /// </summary>
        internal bool HasAsyncOperation => Volatile.Read(ref m_AsyncOperationCount) != 0;

        /// <summary>
        ///   <para>判断当前调用是否来自本页面的异步操作回调。</para>
        /// </summary>
        internal bool IsCurrentAsyncOperation =>
            ReferenceEquals(s_AsyncOperationScope.Value?.Owner, this) &&
            s_AsyncOperationScope.Value.IsActive;

        /// <summary>
        ///   <para>按页面实例串行执行需要等待的异步关闭或释放操作。</para>
        /// </summary>
        /// <param name="operation">待执行的异步操作。</param>
        /// <param name="ct">等待操作开始前使用的取消令牌。</param>
        /// <returns>异步操作结果。</returns>
        /// <remarks>同一回调链不能再次等待当前 <see cref="ViewBase"/> 的异步操作，否则会形成自等待。</remarks>
        /// <typeparam name="TResult">结果类型。</typeparam>
        internal async Task<TResult> RunAsyncOperation<TResult>(
            Func<Task<TResult>> operation,
            CancellationToken ct)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            Game.ThrowIfNotOnMainThread($"{GetType().FullName}.{nameof(RunAsyncOperation)}");
            if (IsCurrentAsyncOperation)
            {
                throw new InvalidOperationException(
                    $"View {GetType().FullName} 的异步操作回调不能再次等待同一 View 的异步操作。");
            }

            Task previous;
            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            lock (m_AsyncOperationLock)
            {
                previous = m_LastAsyncOperation;
                m_LastAsyncOperation = completion.Task;
            }

            Interlocked.Increment(ref m_AsyncOperationCount);

            var entered = false;
            try
            {
                await previous.WaitWithCancellationAsync(ct);
                entered = true;
                var previousScope = s_AsyncOperationScope.Value;
                var scope = new AsyncOperationScope(this);
                s_AsyncOperationScope.Value = scope;
                try
                {
                    return await operation();
                }
                finally
                {
                    // 失效标记会传播到回调创建的未等待任务，防止它们在操作结束后绕过互斥检查。
                    scope.IsActive = false;
                    s_AsyncOperationScope.Value = previousScope;
                }
            }
            catch
            {
                if (!entered)
                {
                    _ = CompleteAsyncOperationAfter(previous, this, completion);
                }

                throw;
            }
            finally
            {
                if (entered)
                {
                    CompleteAsyncOperation(completion);
                }
            }
        }

        /// <summary>
        ///   <para>等待任务结束并更新页面操作状态。</para>
        /// </summary>
        /// <param name="previous">前一个异步操作。</param>
        /// <param name="owner">所有者。</param>
        /// <param name="completion">完成通知。</param>
        private static async Task CompleteAsyncOperationAfter(
            Task previous,
            ViewBase owner,
            TaskCompletionSource<bool> completion)
        {
            await previous;
            owner.CompleteAsyncOperation(completion);
        }

        /// <summary>
        ///   <para>完成异步操作。</para>
        /// </summary>
        /// <param name="completion">完成通知。</param>
        private void CompleteAsyncOperation(TaskCompletionSource<bool> completion)
        {
            if (completion.TrySetResult(true))
            {
                Interlocked.Decrement(ref m_AsyncOperationCount);
            }
        }

        /// <summary>
        ///   <para>异步操作作用域。</para>
        /// </summary>
        private sealed class AsyncOperationScope
        {
            /// <summary>
            ///   <para>所有者。</para>
            /// </summary>
            internal readonly ViewBase Owner;
            /// <summary>
            ///   <para>是否激活。</para>
            /// </summary>
            internal volatile bool IsActive = true;

            /// <summary>
            ///   <para>创建异步操作作用域。</para>
            /// </summary>
            /// <param name="owner">所有者。</param>
            internal AsyncOperationScope(ViewBase owner) => Owner = owner;
        }

        /// <summary>
        ///   <para>页面创建完成后调用一次。</para>
        /// </summary>
        protected virtual void OnCreated() { }

        /// <summary>
        ///   <para>页面创建完成后绑定控件事件。</para>
        /// </summary>
        protected virtual void OnBindEvents() { }

        /// <summary>
        ///   <para>页面打开后调用，每次从关闭状态打开都会调用。</para>
        /// </summary>
        protected virtual void OnOpened() { }

        /// <summary>
        ///   <para>页面关闭后调用。</para>
        /// </summary>
        protected virtual void OnClosed() { }

        /// <summary>
        ///   <para>页面释放前解除控件事件绑定。</para>
        /// </summary>
        protected virtual void OnUnbindEvents() { }

        /// <summary>
        ///   <para>页面进入释放状态后调用一次，用于清理页面自身资源。</para>
        /// </summary>
        protected virtual void OnReleased() { }

        /// <summary>
        ///   <para>取得由当前 <see cref="ViewBase"/> 持有的动态 <see cref="WidgetBase"/>；同一组件重复请求时返回已有实例。</para>
        /// </summary>
        /// <param name="component"><see cref="WidgetBase"/> 实例上的绑定组件。</param>
        /// <typeparam name="TWidget"><see cref="WidgetBase"/> 类型。</typeparam>
        /// <returns>创建完成的 <see cref="WidgetBase"/>。</returns>
        protected TWidget CreateWidget<TWidget>(UIWidgetComponent component)
            where TWidget : WidgetBase
        {
            ThrowIfReleased();
            var widget = UIWidgetOwnerUtility.Create<TWidget>(
                component,
                m_Component?.transform,
                m_Component,
                $"View {GetType().FullName}",
                this,
                out var created);
            try
            {
                if (IsReleased)
                {
                    throw new InvalidOperationException(
                        $"View {GetType().FullName} 在创建 Widget 期间已被释放。");
                }

                if (created)
                {
                    var widgets = m_OwnedWidgets ??= new List<WidgetBase>(2);
                    widgets.Add(widget);
                }
                return widget;
            }
            catch (Exception exception)
            {
                if (!created)
                {
                    throw;
                }

                try
                {
                    widget.Release();
                }
                catch (Exception releaseFailure)
                {
                    throw ExceptionUtility.Combine(exception, releaseFailure);
                }

                throw;
            }
        }

        /// <summary>
        ///   <para>在页面节点上创建或取得同节点同类型的 <see cref="ViewPartBase"/>，并直接传入根组件。</para>
        /// </summary>
        /// <param name="component"><see cref="ViewPartBase"/> 绑定的根组件。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        protected T CreateViewPart<T>(Component component) where T : ViewPartBase
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            ThrowIfReleased();
            PruneReleasedViewParts();
            if (!UICompositionPolicy.IsOwnedNode(m_Component?.transform, component.transform))
            {
                throw new ArgumentException(
                    $"ViewPart 根组件必须位于 View {GetType().FullName} 的 Prefab 节点内。",
                    nameof(component));
            }

            var existing = FindViewPart<T>(component.gameObject);
            if (existing != null)
            {
                return existing;
            }

            var part = ViewPartCreator<T>.Create();
            var parts = m_ViewParts ??= new List<ViewPartBase>(2);
            // 先登记实例，允许 OnCreated 内再次请求同节点同类型时直接复用。
            parts.Add(part);
            try
            {
                part.Attach(this, component);
                return part;
            }
            catch (Exception exception)
            {
                RemoveViewPart(parts, part);
                try
                {
                    part.ReleaseInternal();
                }
                catch (Exception releaseFailure)
                {
                    throw ExceptionUtility.Combine(exception, releaseFailure);
                }

                throw;
            }
        }

        /// <summary>
        ///   <para>移除页面局部组件。</para>
        /// </summary>
        /// <param name="parts">路径片段。</param>
        /// <param name="part">局部组件。</param>
        private static void RemoveViewPart(List<ViewPartBase> parts, ViewPartBase part)
        {
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(parts[i], part))
                {
                    parts.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        ///   <para>按固定索引读取序列化的节点组件。</para>
        /// </summary>
        /// <param name="index">变量索引。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        protected T GetVariable<T>(int index) where T : UnityEngine.Object
            => (m_Component ?? throw new ObjectDisposedException(GetType().FullName)).GetVariable<T>(index);

        /// <summary>
        ///   <para>按相对页面根节点的路径查找子节点。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <returns>找到的节点；路径不存在时返回 null。</returns>
        protected Transform FindChild(string path)
            => (m_Component ?? throw new ObjectDisposedException(GetType().FullName))
                .transform.Find(path);

        /// <summary>
        ///   <para>按相对页面根节点的路径查找子节点组件。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <typeparam name="T">目标组件类型。</typeparam>
        /// <returns>找到的组件；路径不存在或节点未挂载该组件时返回 null。</returns>
        /// <remarks>适合创建阶段或低频查找；高频访问应保存结果或使用生成变量。</remarks>
        protected T FindChildComponent<T>(string path) where T : Component
            => FindChild(path)?.GetComponent<T>();

        /// <summary>
        ///   <para>绑定 Prefab 根节点上的 <see cref="UIViewComponent"/>。</para>
        /// </summary>
        /// <param name="component">Prefab 根节点组件。</param>
        internal void AttachComponent(UIViewComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (m_Component != null || m_State == ViewState.Released)
            {
                throw new InvalidOperationException($"View {GetType().FullName} 已绑定组件或已经释放。");
            }

            if (VariableSignature != component.VariableSignature)
            {
                throw new InvalidOperationException(
                    $"{GetType().FullName} 的 UI 变量与 Prefab 不一致，请在 {nameof(UIViewComponent)} 中重新生成。");
            }
            
            var previousState = m_State;
            var attached = false;
            try
            {
                component.AttachView(this);
                attached = true;
                m_Component = component;
                m_State = ViewState.Attached;
                component.Destroyed += OnComponentDestroyed;
            }
            catch
            {
                if (attached)
                {
                    component.DetachView(this);
                }

                if (ReferenceEquals(m_Component, component))
                {
                    component.Destroyed -= OnComponentDestroyed;
                    m_Component = null;
                    m_State = previousState;
                }

                throw;
            }
        }

        /// <summary>
        ///   <para>完成页面创建并绑定控件事件。</para>
        /// </summary>
        internal void CreateInternal()
        {
            ThrowIfReleased();
            if (m_State == ViewState.Created ||
                m_State == ViewState.Opened ||
                m_State == ViewState.Closed)
            {
                return;
            }

            var previousState = m_State;
            try
            {
                m_State = ViewState.Created;
                OnCreated();
                if (IsReleased)
                {
                    throw new InvalidOperationException(
                        $"View {GetType().FullName} 在创建期间被释放。");
                }

                BindEventsInternal();
                if (IsReleased)
                {
                    throw new InvalidOperationException(
                        $"View {GetType().FullName} 在绑定控件事件期间被释放。");
                }
            }
            catch
            {
                if (m_State != ViewState.Released)
                {
                    m_State = previousState;
                }

                throw;
            }
        }

        /// <summary>
        ///   <para>执行一次打开流程。</para>
        /// </summary>
        /// <param name="args">本次打开参数。</param>
        internal void OpenInternal(ViewArgs args)
        {
            ThrowIfReleased();
            if (m_IsClosing)
            {
                throw new InvalidOperationException(
                    $"View {GetType().FullName} 正在关闭，不能在 OnClosed 回调期间重新打开。");
            }

            if (m_State == ViewState.Opened)
            {
                if (!ReferenceEquals(m_OpenArgs, args))
                    throw new InvalidOperationException($"View {GetType().FullName} 已打开且参数实例不同，请先关闭再打开，或创建独立实例。");
                return;
            }

            var previousState = m_State;
            m_IsOpening = true;
            m_OpenArgs = args;
            try
            {
                m_State = ViewState.Opened;
                OnOpened();
                EnsureOpenAlive();

                // 回调期间允许请求 Close，待页面完成 Open 通知后立即执行。
                m_IsOpening = false;
                Notify(ViewState.Opened);
                EnsureOpenAlive();

                if (m_CloseRequested && m_State != ViewState.Released)
                {
                    m_CloseRequested = false;
                    CloseInternal();
                }
            }
            catch
            {
                m_IsOpening = false;
                m_CloseRequested = false;
                m_ReleaseRequested = false;
                m_OpenArgs = null;
                if (m_State != ViewState.Released)
                {
                    m_State = previousState;
                }

                throw;
            }
        }

        /// <summary>
        ///   <para>执行关闭流程；关闭中的重入请求会延后处理。</para>
        /// </summary>
        internal void CloseInternal()
        {
            if (m_IsOpening)
            {
                m_CloseRequested = true;
                return;
            }

            if (m_State != ViewState.Opened || IsReleased)
            {
                return;
            }

            if (m_IsClosing)
            {
                return;
            }

            m_IsClosing = true;
            Exception failure = null;
            try
            {
                m_State = ViewState.Closed;
                m_OpenArgs = null;
                OnClosed();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            finally
            {
                m_IsClosing = false;
                if (m_State != ViewState.Released)
                {
                    m_State = ViewState.Closed;
                }

                m_OpenArgs = null;
                try { Notify(ViewState.Closed); }
                catch (Exception notificationFailure)
                {
                    failure = ExceptionUtility.Combine(failure, notificationFailure);
                }

                if (m_ReleaseRequested && m_State != ViewState.Released)
                {
                    m_ReleaseRequested = false;
                    try
                    {
                        ReleaseInternal();
                    }
                    catch (Exception releaseFailure)
                    {
                        failure = ExceptionUtility.Combine(failure, releaseFailure);
                    }
                }
            }

            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>释放 <see cref="ViewBase"/>、<see cref="ViewPartBase"/> 和绑定节点。</para>
        /// </summary>
        internal void ReleaseInternal()
        {
            if (m_State == ViewState.Released)
            {
                return;
            }

            if (m_IsOpening)
            { 
                m_ReleaseRequested = true;
                m_CloseRequested = true;
                return;
            }

            if (m_IsClosing)
            {
                m_ReleaseRequested = true;
                return;
            }

            if (m_IsReleasing)
            {
                return;
            }

            m_IsReleasing = true;
            Exception failure = null;
            try
            {
                try
                {
                    CloseInternal();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                m_State = ViewState.Released;
                m_OpenArgs = null;
                try
                {
                    UnbindEventsInternal();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }
                try
                {
                    ReleaseViewParts();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }
                try
                {
                    ReleaseWidgets();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }
                try
                {
                    OnReleased();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }
            }
            finally
            {
                // 状态通知和解绑属于最终清理，不能因用户回调异常而跳过。
                try
                {
                    Notify(ViewState.Released);
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }

                try
                {
                    DetachComponent();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }

                m_IsReleasing = false;
            }

            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>释放部件。</para>
        /// </summary>
        private void ReleaseWidgets() => UIWidgetOwnerUtility.ReleaseAll(ref m_OwnedWidgets);

        /// <summary>
        ///   <para>释放页面局部组件。</para>
        /// </summary>
        private void ReleaseViewParts()
        {
            if (m_ViewParts == null)
            {
                return;
            }

            Exception failure = null;
            for (var i = m_ViewParts.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_ViewParts[i]?.ReleaseInternal();
                }
                catch (Exception exception)
                {
                    failure = ExceptionUtility.Combine(failure, exception);
                }
            }

            m_ViewParts.Clear();
            m_ViewParts = null;
            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>查找 <see cref="ViewPartBase"/> 对象。</para>
        /// </summary>
        /// <param name="gameObject"><see cref="UnityEngine.GameObject"/>。</param>
        /// <typeparam name="T">数据类型。</typeparam>
        private T FindViewPart<T>(GameObject gameObject) where T : ViewPartBase
        {
            PruneReleasedViewParts();
            if (m_ViewParts == null)
            {
                return null;
            }

            for (var i = 0; i < m_ViewParts.Count; i++)
            {
                var part = m_ViewParts[i];
                if (part != null && !part.IsReleased &&
                    part.GetType() == typeof(T) &&
                    ReferenceEquals(part.GameObject, gameObject))
                {
                    return (T)part;
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>清理根节点已被 Unity 销毁的 <see cref="ViewPartBase"/>，释放其仍持有的子资源。</para>
        /// </summary>
        private void PruneReleasedViewParts()
        {
            if (m_IsReleasing || m_IsPruningViewParts || m_ViewParts == null)
            {
                return;
            }

            m_IsPruningViewParts = true;
            List<Exception> failures = null;
            try
            {
                for (var i = m_ViewParts.Count - 1; i >= 0; i--)
                {
                    var part = m_ViewParts[i];
                    if (part != null && !part.IsReleased)
                    {
                        continue;
                    }

                    if (part != null)
                    {
                        try
                        {
                            part.ReleaseInternal();
                        }
                        catch (Exception exception)
                        {
                            ExceptionUtility.Add(ref failures, exception);
                        }
                    }

                    m_ViewParts.RemoveAt(i);
                }

                if (m_ViewParts.Count == 0)
                {
                    m_ViewParts = null;
                }
            }
            finally
            {
                m_IsPruningViewParts = false;
            }
            ExceptionUtility.ThrowIfAny(failures);
        }

        /// <summary>
        ///   <para>处理组件已销毁。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        private void OnComponentDestroyed(UIViewComponent component)
        {
            if (!ReferenceEquals(m_Component, component) || m_State == ViewState.Released)
            {
                return;
            }

            m_ReleaseRequested = true;
            if (m_IsClosing)
            {
                return;
            }

            ReleaseInternal();
        }

        /// <summary>
        ///   <para>绑定事件。</para>
        /// </summary>
        private void BindEventsInternal()
        {
            if (m_EventsBound)
            {
                return;
            }

            m_EventsBound = true;
            try
            {
                OnBindEvents();
            }
            catch (Exception exception)
            {
                if (m_EventsBound)
                {
                    try
                    {
                        OnUnbindEvents();
                    }
                    catch (Exception unbindFailure)
                    {
                        exception = ExceptionUtility.Combine(exception, unbindFailure);
                    }

                    m_EventsBound = false;
                }

                throw exception;
            }
        }

        /// <summary>
        ///   <para>解绑事件。</para>
        /// </summary>
        private void UnbindEventsInternal()
        {
            if (!m_EventsBound)
            {
                return;
            }

            try
            {
                OnUnbindEvents();
            }
            finally
            {
                m_EventsBound = false;
            }
        }

        /// <summary>
        ///   <para>解除绑定组件。</para>
        /// </summary>
        private void DetachComponent()
        {
            var component = m_Component;
            m_Component = null;
            if (component != null)
            {
                component.Destroyed -= OnComponentDestroyed;
                component.DetachView(this);
            }

            StateChanged = null;
        }

        /// <summary>
        ///   <para>已释放时抛出异常。</para>
        /// </summary>
        private void ThrowIfReleased()
        {
            if (IsReleased)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        ///   <para>检查页面打开状态仍然有效。</para>
        /// </summary>
        private void EnsureOpenAlive()
        {
            if (!IsReleased)
            {
                return;
            }

            // 回调可能销毁页面节点；先结束打开阶段，再补齐关闭和释放生命周期。
            m_IsOpening = false;
            if (m_State != ViewState.Released)
            {
                ReleaseInternal();
            }

            throw new InvalidOperationException(
                $"View {GetType().FullName} 在打开期间被释放。");
        }

        /// <summary>
        ///   <para>通知。</para>
        /// </summary>
        /// <param name="state">状态。</param>
        private void Notify(ViewState state)
        {
            var callback = StateChanged;
            if (callback == null)
            {
                return;
            }

            List<Exception> failures = null;
            foreach (Action<ViewBase, ViewState> handler in callback.GetInvocationList())
            {
                try
                {
                    handler(this, state);
                }
                catch (Exception exception)
                {
                    ExceptionUtility.Add(ref failures, exception);
                }
            }
            ExceptionUtility.ThrowIfAny(failures);
        }
    }

    /// <summary>
    ///   <para>页面打开参数；按引用传递，由页面借用。</para>
    /// </summary>
    public abstract class ViewArgs { }

    /// <summary>
    ///   <para>有参数页面基类；强类型读取打开参数，生命周期由模块管理。</para>
    /// </summary>
    /// <typeparam name="TArgs">参数类型；仅借用，不转移业务资源所有权。</typeparam>
    public abstract class ViewBase<TArgs> : ViewBase where TArgs : ViewArgs
    {
        /// <summary>
        ///   <para>本次打开参数；关闭后为空。</para>
        /// </summary>
        public TArgs OpenArgs => (TArgs)OpenArgsInternal;

        /// <inheritdoc />
        protected sealed override void OnOpened() => OnOpened(OpenArgs);

        /// <summary>
        ///   <para>页面打开后调用。</para>
        /// </summary>
        /// <param name="args">本次打开参数。</param>
        protected virtual void OnOpened(TArgs args) { }
    }

    /// <summary>
    ///   <para>UI 程序集内部的 <see cref="ViewBase"/> 创建器。</para>
    /// </summary>
    internal static class ViewCreator
    {
        /// <summary>
        ///   <para>创建 <see cref="ViewBase"/> 类型实例。</para>
        /// </summary>
        /// <param name="viewType"><see cref="ViewBase"/> 类型。</param>
        internal static ViewBase Create(Type viewType) => (ViewBase)UIInstanceCreator.Create(viewType, typeof(ViewBase), "View");
    }
}
