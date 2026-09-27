#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>UI 模块；管理页面资源、实例和显示生命周期。</para>
    /// </summary>
    [Serializable, GameModule("UI 视图模块"), GameModuleDependency(typeof(ILoader))]
    public sealed partial class UIModule : GameModule, IUIManager
    {
        /// <summary>
        ///   <para>参考分辨率。</para>
        /// </summary>
        [SerializeField] private Vector2 m_ReferenceResolution = new(1920f, 1080f);
        /// <summary>
        ///   <para>宽高适配权重。</para>
        /// </summary>
        [SerializeField, Range(0f, 1f)] private float m_MatchWidthOrHeight = 0.5f;
        /// <summary>
        ///   <para>关闭页面的缓存上限。</para>
        /// </summary>
        [SerializeField, Min(0)] private int m_KeepAliveLimit;

        /// <summary>
        ///   <para>页面根节点。</para>
        /// </summary>
        private Transform m_ViewRoot;
        /// <summary>
        ///   <para>页面条目。</para>
        /// </summary>
        private readonly Dictionary<Type, List<ViewEntry>> m_ViewEntries = new();
        /// <summary>
        ///   <para>页面到条目的映射。</para>
        /// </summary>
        private readonly Dictionary<ViewBase, ViewEntry> m_ViewLookup =
            new(ReferenceEqualityComparer<ViewBase>.Instance);
        /// <summary>
        ///   <para>正在创建实例的页面类型。</para>
        /// </summary>
        private readonly HashSet<Type> m_CreatingTypes = new();
        /// <summary>
        ///   <para>打开操作。</para>
        /// </summary>
        private readonly List<OpenOperation> m_OpenOperations = new();
        /// <summary>
        ///   <para>层级根节点。</para>
        /// </summary>
        private readonly Dictionary<UILayer, Transform> m_LayerRoots = new();
        /// <summary>
        ///   <para>淘汰候选。</para>
        /// </summary>
        private readonly List<ViewEntry> m_EvictionCandidates = new();
        /// <summary>
        ///   <para>页面选项。</para>
        /// </summary>
        private static readonly Dictionary<Type, ViewOptions> s_ViewOptions = new();
        /// <summary>
        ///   <para>参数类型缓存；无参数页面记录为空，仅在首次打开时检查继承链。</para>
        /// </summary>
        private static readonly Dictionary<Type, Type> s_ViewArgsTypes = new();
        /// <summary>
        ///   <para>加载器。</para>
        /// </summary>
        private ILoader m_Loader;
        /// <summary>
        ///   <para>生命周期取消源。</para>
        /// </summary>
        private CancellationTokenSource m_Lifetime;
        /// <summary>
        ///   <para>根节点所有权标记。</para>
        /// </summary>
        private bool m_OwnsViewRoot;
        /// <summary>
        ///   <para>页面根节点已锁定。</para>
        /// </summary>
        private bool m_ViewRootLocked;
        /// <summary>
        ///   <para>缓存访问顺序。</para>
        /// </summary>
        private long m_CacheAccessOrder;
        /// <summary>
        ///   <para>下一个实例标识。</para>
        /// </summary>
        private long m_NextInstanceId;
        /// <summary>
        ///   <para>是否正在淘汰缓存页面。</para>
        /// </summary>
        private bool m_IsEvictingCache;
        /// <summary>
        ///   <para>模态遮罩。</para>
        /// </summary>
        private UIModalLayer m_ModalLayer;

        /// <summary>
        ///   <para>返回 UI 根节点。</para>
        /// </summary>
        public Transform ViewRoot => m_ViewRoot;

        /// <summary>
        ///   <para>返回全屏输入阻断层。</para>
        /// </summary>
        public UIModalLayer ModalLayer => m_ModalLayer;

        /// <summary>
        ///   <para>返回自动根 <see cref="UnityEngine.Canvas"/> 的参考分辨率。</para>
        /// </summary>
        public Vector2 ReferenceResolution => m_ReferenceResolution;

        /// <summary>
        ///   <para>返回自动根 <see cref="UnityEngine.Canvas"/> 的宽高匹配比例。</para>
        /// </summary>
        public float MatchWidthOrHeight => m_MatchWidthOrHeight;

        /// <summary>
        ///   <para>设置 <see cref="UIViewCacheMode.KeepAlive"/> 缓存的最大关闭视图数量。</para>
        /// </summary>
        public int KeepAliveLimit
        {
            get => m_KeepAliveLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "KeepAlive 数量上限不能小于 0。");
                }

                Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(KeepAliveLimit)}");
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(UIModule));
                }

                m_KeepAliveLimit = value;
                if (IsInstalled)
                {
                    TrimKeepAliveCache();
                }
            }
        }

        /// <summary>
        ///   <para>返回已创建的视图数量。</para>
        /// </summary>
        public int ViewCount
        {
            get
            {
                Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(ViewCount)}");
                var count = 0;
                foreach (var entries in m_ViewEntries.Values)
                {
                    for (var i = 0; i < entries.Count; i++)
                    {
                        if (IsUsableEntry(entries[i]))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        /// <summary>
        ///   <para>返回当前打开的视图数量。</para>
        /// </summary>
        public int OpenViewCount
        {
            get
            {
                Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(OpenViewCount)}");
                var count = 0;
                foreach (var entries in m_ViewEntries.Values)
                {
                    foreach (var entry in entries)
                    {
                        if (entry.View != null && entry.View.IsOpen)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        /// <summary>
        ///   <para>当前正在打开的视图任务数量。</para>
        /// </summary>
        public int OpeningViewCount
        {
            get
            {
                Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(OpeningViewCount)}");
                return m_OpenOperations.Count;
            }
        }

        /// <inheritdoc />
        public ViewBase Open(
            Type viewType,
            ViewArgs args = null,
            Transform parent = null,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
        {
            var createNew = PrepareOpen(viewType, args, openMode, nameof(Open));
            var options = GetViewOptions(viewType);
            return OpenSync(viewType, options, parent, args, createNew);
        }

        /// <inheritdoc />
        public Task<ViewBase> OpenAsync(
            Type viewType,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
        {
            var createNew = PrepareOpen(viewType, args, openMode, nameof(OpenAsync));
            var options = GetViewOptions(viewType);
            return BeginOpenAsync(viewType, options, parent, args, ct, createNew);
        }

        /// <inheritdoc />
        public ViewBase Open(
            Type viewType,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            ViewArgs args = null,
            Transform parent = null,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
        {
            var createNew = PrepareOpen(viewType, args, openMode, nameof(Open));
            var options = CreateViewOptions(assetPath, layer, cacheMode);
            return OpenSync(viewType, options, parent, args, createNew);
        }

        /// <inheritdoc />
        public Task<ViewBase> OpenAsync(
            Type viewType,
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode,
            ViewArgs args = null,
            Transform parent = null,
            CancellationToken ct = default,
            UIViewOpenMode openMode = UIViewOpenMode.Reuse)
        {
            var createNew = PrepareOpen(viewType, args, openMode, nameof(OpenAsync));
            var options = CreateViewOptions(assetPath, layer, cacheMode);
            return BeginOpenAsync(viewType, options, parent, args, ct, createNew);
        }

        /// <inheritdoc />
        public bool TryGetView(Type viewType, out ViewBase view)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(TryGetView)}");
            if (TryGetSingleView(viewType, out var entry))
            {
                view = entry.View;
                return true;
            }

            view = null;
            return false;
        }

        /// <inheritdoc />
        public bool Close(Type viewType)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(Close)}");
            if (!TryGetSingleView(viewType, out var entry))
            {
                return false;
            }

            CloseEntry(entry);
            TrimKeepAliveCache();
            return true;
        }

        /// <inheritdoc />
        public bool Close(ViewBase view)
        {
            if (view == null)
            {
                return false;
            }

            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(Close)}");
            if (!TryFindEntry(view, out var entry) || entry.View.IsReleased || entry.View.Component == null)
            {
                return false;
            }

            CloseEntry(entry);
            TrimKeepAliveCache();
            return true;
        }

        /// <inheritdoc />
        public bool Release(Type viewType)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(Release)}");
            if (!TryGetSingleView(viewType, out var entry))
            {
                return false;
            }

            ReleaseEntry(entry);
            return true;
        }

        /// <inheritdoc />
        public bool Release(ViewBase view)
        {
            if (view == null)
            {
                return false;
            }

            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(Release)}");
            if (!TryFindEntry(view, out var entry) || entry.View.IsReleased)
            {
                return false;
            }

            ReleaseEntry(entry);
            return true;
        }

        /// <inheritdoc />
        public void CloseAll()
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(CloseAll)}");
            CloseAllInternal(layer: null);
        }

        /// <inheritdoc />
        public void CloseAll(UILayer layer)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(CloseAll)}");
            CloseAllInternal(layer);
        }

        /// <inheritdoc />
        public void ReleaseAll()
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(ReleaseAll)}");
            ReleaseAllInternal(layer: null);
        }

        /// <inheritdoc />
        public void ReleaseAll(UILayer layer)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(ReleaseAll)}");
            ReleaseAllInternal(layer);
        }

        /// <inheritdoc />
        public void CopyViewDebugInfoTo(List<UIViewDebugInfo> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(CopyViewDebugInfoTo)}");
            output.Clear();
            foreach (var entries in m_ViewEntries.Values)
            {
                foreach (var view in entries)
                {
                    if (!IsUsableEntry(view))
                    {
                        continue;
                    }

                    output.Add(new UIViewDebugInfo(
                        view.InstanceId,
                        view.ViewType,
                        view.View.ViewName,
                        view.Options.AssetPath,
                        view.Options.Layer,
                        view.Options.CacheMode,
                        view.View.IsOpen));
                }
            }
        }

        /// <inheritdoc />
        public void GetViews(Type viewType, IList output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(GetViews)}");
            output.Clear();
            if (!IsViewType(viewType))
            {
                return;
            }

            PruneInvalidEntriesForType(viewType);
            if (!m_ViewEntries.TryGetValue(viewType, out var entries))
            {
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (IsUsableEntry(entry))
                {
                    output.Add(entry.View);
                }
            }
        }

        /// <summary>
        ///   <para>同步打开页面。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="options">页面配置。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="args">打开参数。</param>
        /// <param name="createNew">是否创建独立实例。</param>
        private ViewBase OpenSync(Type viewType, ViewOptions options, Transform parent, ViewArgs args, bool createNew)
        {
            if (FindOpenOperation(viewType) is { } opening && (!createNew || !opening.CreateNew))
                throw new InvalidOperationException($"View 类型 {viewType.FullName} 正在异步加载，请等待加载完成。");
            if (!createNew && TryOpenCached(viewType, options, parent, args, out var cached)) return cached;

            // 加载器也可能重入 UI；加载开始后即锁定当前类型和根节点。
            m_CreatingTypes.Add(viewType);
            try
            {
                return CreateAndOpen(viewType, m_Loader.LoadAsset<GameObject>(options.AssetPath), options, parent, args);
            }
            finally { m_CreatingTypes.Remove(viewType); }
        }

        /// <summary>
        ///   <para>开始异步打开页面；复用相同请求，独立实例分别加载。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="options">页面配置。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="args">打开参数。</param>
        /// <param name="ct">调用方的取消令牌。</param>
        /// <param name="createNew">是否创建独立实例。</param>
        private Task<ViewBase> BeginOpenAsync(
            Type viewType, ViewOptions options, Transform parent, ViewArgs args, CancellationToken ct, bool createNew)
        {
            ct.ThrowIfCancellationRequested();
            if (FindOpenOperation(viewType) is { } opening)
            {
                if (opening.CreateNew != createNew || !createNew && !opening.Matches(options, parent, args))
                    throw new InvalidOperationException($"View 类型 {viewType.FullName} 正在执行不同的打开请求，请等待加载完成。");
                if (!createNew) return opening.Completion.Task.WaitWithCancellationAsync(ct);
            }
            if (!createNew && TryOpenCached(viewType, options, parent, args, out var cached))
                return Task.FromResult(cached);

            var operation = new OpenOperation(viewType, options, parent, args, createNew);
            m_OpenOperations.Add(operation);
            _ = LoadAndOpenAsync(operation, createNew ? ct : CancellationToken.None);
            return operation.Completion.Task.WaitWithCancellationAsync(ct);
        }

        /// <summary>
        ///   <para>加载并打开页面；结束跟踪后交付结果。</para>
        /// </summary>
        /// <param name="opening">已登记的打开操作。</param>
        /// <param name="requestCt">独立实例的取消令牌。</param>
        private async Task LoadAndOpenAsync(OpenOperation opening, CancellationToken requestCt)
        {
            AssetLoadHandle<GameObject> handle = null;
            ViewBase view = null;
            Exception failure = null;
            try
            {
                var token = m_Lifetime.Token;
                handle = await m_Loader.LoadAssetAsync<GameObject>(opening.Options.AssetPath, token);
                token.ThrowIfCancellationRequested();
                requestCt.ThrowIfCancellationRequested();
                if (!m_CreatingTypes.Add(opening.ViewType))
                    throw new InvalidOperationException($"View 类型 {opening.ViewType.FullName} 正在创建，不能重入创建同类型页面。");
                try
                {
                    view = CreateAndOpen(opening.ViewType, handle, opening.Options, opening.Parent, opening.Args);
                    handle = null;
                }
                finally { m_CreatingTypes.Remove(opening.ViewType); }
                if (requestCt.IsCancellationRequested)
                {
                    try { Release(view); }
                    catch (Exception releaseFailure)
                    {
                        throw ExceptionUtility.Combine(new OperationCanceledException(requestCt), releaseFailure);
                    }
                    requestCt.ThrowIfCancellationRequested();
                }
            }
            catch (Exception exception)
            {
                failure = exception;
                try { handle?.Dispose(); }
                catch (Exception releaseFailure) { failure = ExceptionUtility.Combine(failure, releaseFailure); }
            }
            finally
            {
                opening.Args = default;
                m_OpenOperations.Remove(opening);
            }

            if (failure is OperationCanceledException canceled)
                opening.Completion.TrySetCanceled(canceled.CancellationToken);
            else if (failure != null)
            {
                opening.Completion.SetException(failure);
                // 等待者可能已取消等待；结果仍保留错误，同时观察异常避免遗留未观察任务。
                _ = opening.Completion.Task.Exception;
            }
            else opening.Completion.SetResult(view);
        }

        /// <summary>
        ///   <para>接管加载句柄并创建页面；失败时释放全部已创建资源。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="handle">加载句柄。</param>
        /// <param name="options">页面配置。</param>
        /// <param name="parent">父节点。</param>
        /// <param name="args">打开参数。</param>
        private ViewBase CreateAndOpen(
            Type viewType, AssetLoadHandle<GameObject> handle, ViewOptions options, Transform parent, ViewArgs args)
        {
            GameObject instance = null;
            ViewEntry entry = null;
            try
            {
                Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{nameof(CreateAndOpen)}");
                (m_Lifetime ?? throw new ObjectDisposedException(nameof(UIModule))).Token.ThrowIfCancellationRequested();
                if (handle == null || handle.Result == null)
                    throw new InvalidOperationException($"无法加载 UI Prefab：{options.AssetPath}");
                ValidateViewParent(parent, null);
                instance = UnityEngine.Object.Instantiate(handle.Result, GetViewParent(parent, options.Layer));
                var component = instance.GetComponent<UIViewComponent>();
                if (component == null)
                    throw new InvalidOperationException($"UI Prefab {options.AssetPath} 未包含 {nameof(UIViewComponent)}。");

                var view = ViewCreator.Create(viewType);
                view.AttachComponent(component);
                entry = new ViewEntry(view, instance, handle, options, ++m_NextInstanceId);
                handle = null;
                view.CreateInternal();
                (m_Lifetime ?? throw new ObjectDisposedException(nameof(UIModule))).Token.ThrowIfCancellationRequested();
                AddView(entry);
                view.StateChanged += OnViewStateChanged;
                BringToFront(component);
                SetViewVisible(component, true);
                view.OpenInternal(args);
                m_ViewRootLocked = true;
                MarkKeepAliveUsed(entry);
                TrimKeepAliveCache();
                return view;
            }
            catch (Exception failure)
            {
                if (entry != null)
                {
                    RemoveView(entry);
                    try { DisposeEntry(entry); }
                    catch (Exception cleanupFailure) { failure = ExceptionUtility.Combine(failure, cleanupFailure); }
                }
                else
                {
                    if (instance != null)
                    {
                        instance.SetActive(false);
                        UnityEngine.Object.Destroy(instance);
                    }
                    try { handle?.Dispose(); }
                    catch (Exception cleanupFailure) { failure = ExceptionUtility.Combine(failure, cleanupFailure); }
                }
                ExceptionUtility.Rethrow(failure);
                throw;
            }
        }

        /// <summary>
        ///   <para>打开已缓存实例并更新父节点、层级和显示顺序。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="options">页面配置。</param>
        /// <param name="parent">父级。</param>
        /// <param name="args">数据。</param>
        /// <param name="view">页面。</param>
        private bool TryOpenCached(
            Type viewType,
            ViewOptions options,
            Transform parent,
            ViewArgs args,
            out ViewBase view)
        {
            if (TryGetSingleView(viewType, out var entry))
            {
                if (!entry.Options.Matches(options))
                    throw new InvalidOperationException($"View 类型 {viewType.FullName} 的已有实例配置不同，请先 Release 再打开，或使用 UIViewOpenMode.New 创建独立实例。");
                var typedView = entry.View;
                if (typedView.IsOpen && !ReferenceEquals(typedView.OpenArgsInternal, args))
                    throw new InvalidOperationException($"View {viewType.FullName} 已打开且参数不同，请先关闭再打开，或创建独立实例。");
                ValidateViewParent(parent, typedView.Component.transform);
                var targetParent = GetViewParent(parent, options.Layer);
                if (typedView.Component != null && typedView.Component.transform.parent != targetParent)
                {
                    typedView.Component.transform.SetParent(targetParent, false);
                }

                BringToFront(entry.View.Component);
                SetViewVisible(typedView.Component, true);
                try
                {
                    var wasOpen = typedView.IsOpen;
                    typedView.OpenInternal(args);
                    if (wasOpen)
                    {
                        m_ViewStack?.Push(typedView);
                    }
                    MarkKeepAliveUsed(entry);
                    TrimKeepAliveCache();
                }
                catch
                {
                    if (IsCurrentView(entry) && !typedView.IsOpen && typedView.Component != null)
                    {
                        SetViewVisible(typedView.Component, false);
                    }

                    throw;
                }

                view = typedView;
                return true;
            }

            view = null;
            return false;
        }

        /// <summary>
        ///   <para>校验页面父级。</para>
        /// </summary>
        /// <param name="parent">父级。</param>
        /// <param name="viewTransform">页面变换。</param>
        private static void ValidateViewParent(Transform parent, Transform viewTransform)
        {
            var error = UICompositionPolicy.GetViewParentError(parent, viewTransform);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(parent));
            }
        }

        /// <summary>
        ///   <para>关闭全部。</para>
        /// </summary>
        /// <param name="layer">层级。</param>
        private void CloseAllInternal(UILayer? layer)
        {
            try
            {
                ForEachView(layer, CloseEntry, "关闭一个或多个 UI 页面时发生错误。");
            }
            finally
            {
                TrimKeepAliveCache();
            }
        }

        /// <summary>
        ///   <para>释放全部。</para>
        /// </summary>
        /// <param name="layer">层级。</param>
        private void ReleaseAllInternal(UILayer? layer) => ForEachView(layer, ReleaseEntry, "释放一个或多个 UI 页面时发生错误。");

        /// <summary>
        ///   <para>遍历页面。</para>
        /// </summary>
        /// <param name="layer">层级。</param>
        /// <param name="action">操作。</param>
        /// <param name="errorMessage">错误消息。</param>
        private void ForEachView(UILayer? layer, Action<ViewEntry> action, string errorMessage)
        {
            List<Exception> errors = null;
            foreach (var entry in CopyViews())
            {
                if ((layer.HasValue && entry.Options.Layer != layer.Value) || !IsCurrentView(entry))
                {
                    continue;
                }

                try
                {
                    action(entry);
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }

            ExceptionUtility.ThrowIfAny(errors, errorMessage);
        }

        /// <summary>
        ///   <para>关闭页面；缓存处理统一由状态通知触发。</para>
        /// </summary>
        /// <param name="entry">页面条目。</param>
        private void CloseEntry(ViewEntry entry)
        {
            ThrowIfAsyncOperationInProgress(entry.View);
            entry.View.CloseInternal();
        }

        /// <summary>
        ///   <para>处理已进入 <see cref="ViewState.Closed"/> 状态的页面缓存或销毁策略。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private void FinalizeClosedEntry(ViewEntry entry)
        {
            if (!IsCurrentView(entry) || entry.View == null || entry.View.IsOpen ||
                entry.View.IsOpening || entry.View.IsClosing)
            {
                return;
            }

            if (entry.Options.CacheMode == UIViewCacheMode.KeepAlive)
            {
                if (entry.View.Component != null)
                {
                    SetViewVisible(entry.View.Component, false);
                }

                MarkKeepAliveUsed(entry);
                return;
            }

            ReleaseEntry(entry);
        }

        /// <summary>
        ///   <para>存在进行中的异步操作时抛出异常。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        private void ThrowIfAsyncOperationInProgress(ViewBase view)
        {
            // 模块终止时资源回收优先于等待业务回调，避免卸载阶段卡住或泄露句柄。
            var isTerminating = m_Lifetime != null && m_Lifetime.IsCancellationRequested;
            if (view != null && view.HasAsyncOperation && !view.IsCurrentAsyncOperation && !isTerminating)
            {
                throw new InvalidOperationException(
                    $"View {view.GetType().FullName} 正在执行异步关闭或释放操作，请等待该操作完成。");
            }
        }

        /// <summary>
        ///   <para>切换页面显示状态；优先禁用根 <see cref="UnityEngine.Canvas"/>，避免整棵节点树重复触发生命周期。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        /// <param name="visible">可见。</param>
        private static void SetViewVisible(UIViewComponent component, bool visible)
        {
            if (component == null || component.gameObject == null)
            {
                return;
            }

            var gameObject = component.gameObject;
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            canvas.enabled = visible;
        }

        /// <summary>
        ///   <para>释放条目。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private void ReleaseEntry(ViewEntry entry)
        {
            if (entry == null || entry.View == null)
            {
                return;
            }

            ThrowIfAsyncOperationInProgress(entry.View);

            // 打开或关闭回调中的释放必须延后，避免提前销毁节点和资源句柄。
            if (entry.View.IsOpening || entry.View.IsClosing)
            {
                entry.View.ReleaseInternal();
                return;
            }

            RemoveView(entry);
            DisposeEntry(entry);
        }

        /// <summary>
        ///   <para>释放条目。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private void DisposeEntry(ViewEntry entry)
        {
            Exception failure = null;
            if (entry.View != null)
            {
                entry.View.StateChanged -= OnViewStateChanged;

                try
                {
                    entry.View.ReleaseInternal();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }

            failure = ExceptionUtility.Combine(failure, DisposeEntryResources(entry));

            if (failure != null)
            {
                throw failure;
            }
        }

        /// <summary>
        ///   <para>停用并销毁页面实例，释放资源句柄；生命周期回调由 <see cref="ViewBase"/> 负责。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private static Exception DisposeEntryResources(ViewEntry entry)
        {
            Exception failure = null;
            try
            {
                if (entry.Instance != null)
                {
                    entry.Instance.SetActive(false);
                    UnityEngine.Object.Destroy(entry.Instance);
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            try
            {
                entry.Handle?.Dispose();
            }
            catch (Exception exception)
            {
                failure = ExceptionUtility.Combine(failure, exception);
            }

            return failure;
        }

        /// <summary>
        ///   <para>处理页面状态变化。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        /// <param name="state">状态。</param>
        private void OnViewStateChanged(ViewBase view, ViewState state)
        {
            if (view == null)
            {
                return;
            }

            switch (state)
            {
                case ViewState.Opened:
                    m_ViewStack?.Push(view);
                    return;
                case ViewState.Closed:
                    m_ViewStack?.Remove(view);
                    if (TryFindEntry(view, out var closedEntry))
                    {
                        FinalizeClosedEntry(closedEntry);
                    }
                    return;
                case ViewState.Released:
                    m_ViewStack?.Remove(view);
                    break;
                default:
                    return;
            }

            if (!TryFindEntry(view, out var entry))
            {
                return;
            }

            RemoveView(entry);
            var failure = DisposeEntryResources(entry);
            ExceptionUtility.Rethrow(failure);
        }

        /// <summary>
        ///   <para>查找指定类型正在执行的打开操作。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        private OpenOperation FindOpenOperation(Type viewType)
        {
            foreach (var opening in m_OpenOperations)
                if (opening.ViewType == viewType) return opening;
            return null;
        }

        /// <summary>
        ///   <para>取得指定类型的唯一页面；多实例时拒绝歧义操作。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="entry">页面条目。</param>
        private bool TryGetSingleView(Type viewType, out ViewEntry entry)
        {
            PruneInvalidEntriesForType(viewType);
            entry = null;
            if (!IsViewType(viewType) || !m_ViewEntries.TryGetValue(viewType, out var entries)) return false;
            if (entries.Count > 1)
                throw new InvalidOperationException($"View 类型 {viewType.FullName} 存在多个实例，请传入具体 View 实例或使用批量操作。");
            entry = entries[0];
            return true;
        }

        /// <summary>
        ///   <para>判断是否为可用条目。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private static bool IsUsableEntry(ViewEntry entry)
        {
            return entry != null && entry.View != null &&
                   !entry.View.IsReleased && entry.View.Component != null;
        }

        /// <summary>
        ///   <para>回收节点已销毁的页面；先固定清理范围，允许释放回调重入。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        private void PruneInvalidEntriesForType(Type viewType)
        {
            if (!IsViewType(viewType) || !m_ViewEntries.TryGetValue(viewType, out var entries)) return;
            List<ViewEntry> invalid = null;
            foreach (var entry in entries)
                if (!IsUsableEntry(entry)) (invalid ??= new List<ViewEntry>()).Add(entry);
            if (invalid == null) return;
            ResourceUtility.ReleaseAll(invalid, entry =>
            {
                if (!IsCurrentView(entry)) return;
                RemoveView(entry);
                DisposeEntry(entry);
            });
        }

        /// <summary>
        ///   <para>尝试查找条目。</para>
        /// </summary>
        /// <param name="view">页面。</param>
        /// <param name="entry">条目。</param>
        private bool TryFindEntry(ViewBase view, out ViewEntry entry)
        {
            entry = null;
            return view != null && m_ViewLookup.TryGetValue(view, out entry);
        }

        /// <summary>
        ///   <para>判断是否为当前页面。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private bool IsCurrentView(ViewEntry entry)
        {
            return entry != null && entry.View != null &&
                   m_ViewLookup.TryGetValue(entry.View, out var current) &&
                   ReferenceEquals(current, entry);
        }

        /// <summary>
        ///   <para>移除页面。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private void RemoveView(ViewEntry entry)
        {
            if (m_ViewEntries.TryGetValue(entry.ViewType, out var entries))
            {
                entries.Remove(entry);

                if (entries.Count == 0)
                {
                    m_ViewEntries.Remove(entry.ViewType);
                }
            }

            if (entry.View != null && m_ViewLookup.TryGetValue(entry.View, out var current) &&
                ReferenceEquals(current, entry))
            {
                m_ViewLookup.Remove(entry.View);
            }
        }

        /// <summary>
        ///   <para>添加页面。</para>
        /// </summary>
        /// <param name="entry">条目。</param>
        private void AddView(ViewEntry entry)
        {
            if (!m_ViewEntries.TryGetValue(entry.ViewType, out var entries))
            {
                entries = new List<ViewEntry>(1);
                m_ViewEntries.Add(entry.ViewType, entries);
            }

            entries.Add(entry);
            m_ViewLookup[entry.View] = entry;
        }

        /// <summary>
        ///   <para>检查模块已就绪。</para>
        /// </summary>
        /// <param name="operation">操作。</param>
        private void EnsureReady(string operation)
        {
            if (!IsInstalled || m_Loader == null)
            {
                throw new InvalidOperationException(
                    $"执行 {operation} 前必须先安装 {nameof(UIModule)}。");
            }
        }

        /// <summary>
        ///   <para>准备打开。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="openMode">打开模式。</param>
        /// <param name="operation">操作。</param>
        /// <param name="args">打开参数。</param>
        private bool PrepareOpen(Type viewType, ViewArgs args, UIViewOpenMode openMode, string operation)
        {
            Game.ThrowIfNotOnMainThread($"{nameof(UIModule)}.{operation}");
            ValidateViewType(viewType);
            ValidateViewArgs(viewType, args);
            if (m_CreatingTypes.Contains(viewType))
            {
                throw new InvalidOperationException(
                    $"View 类型 {viewType.FullName} 正在创建，不能在创建回调期间递归打开自身。");
            }

            var createNew = ValidateOpenMode(openMode);
            EnsureReady(operation);
            return createNew;
        }

        /// <summary>
        ///   <para>在加载前校验参数；有参数页面不可省略，无参数页面不可多传。</para>
        /// </summary>
        /// <param name="viewType">已验证的页面类型。</param>
        /// <param name="args">打开参数。</param>
        private static void ValidateViewArgs(Type viewType, ViewArgs args)
        {
            if (!s_ViewArgsTypes.TryGetValue(viewType, out var argsType))
            {
                for (var type = viewType.BaseType; type != typeof(ViewBase); type = type.BaseType)
                {
                    if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ViewBase<>)) continue;
                    argsType = type.GetGenericArguments()[0];
                    break;
                }
                s_ViewArgsTypes.Add(viewType, argsType);
            }

            if (argsType == null)
            {
                if (args != null) throw new ArgumentException($"View {viewType.FullName} 不接受打开参数。", nameof(args));
            }
            else if (args == null)
                throw new ArgumentNullException(nameof(args), $"View {viewType.FullName} 需要 {argsType.FullName} 参数。");
            else if (!argsType.IsInstanceOfType(args))
                throw new ArgumentException($"View {viewType.FullName} 需要 {argsType.FullName}，实际传入 {args.GetType().FullName}。", nameof(args));
        }

        /// <summary>
        ///   <para>复制当前页面条目，允许批量操作期间重入。</para>
        /// </summary>
        private List<ViewEntry> CopyViews() => new(m_ViewLookup.Values);

        /// <summary>
        ///   <para>获取页面选项。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        private static ViewOptions GetViewOptions(Type viewType)
        {
            if (!s_ViewOptions.TryGetValue(viewType, out var options))
            {
                var attribute = Attribute.GetCustomAttribute(viewType, typeof(ViewConfigAttribute), true) as ViewConfigAttribute;
                if (attribute == null)
                {
                    throw new InvalidOperationException(
                        $"View 类型 {viewType.FullName} 没有 {nameof(ViewConfigAttribute)}，请显式传入资源路径。");
                }

                // 特性配置和显式路径必须经过同一套规范化与枚举校验，避免反斜杠或首尾空格导致加载失败。
                try
                {
                    options = CreateViewOptions(attribute.AssetPath, attribute.Layer, attribute.CacheMode);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        $"View 类型 {viewType.FullName} 的 {nameof(ViewConfigAttribute)} 无效：{exception.Message}",
                        exception);
                }

                s_ViewOptions.Add(viewType, options);
            }

            return options;
        }

        /// <summary>
        ///   <para>创建页面选项。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="layer">层级。</param>
        /// <param name="cacheMode">关闭后的缓存模式。</param>
        private static ViewOptions CreateViewOptions(
            string assetPath,
            UILayer layer,
            UIViewCacheMode cacheMode)
        {
            assetPath = Game.PathUtility.Normalize(assetPath);
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("UI Prefab 资源路径不能为空。", nameof(assetPath));
            }

            if (!Enum.IsDefined(typeof(UILayer), layer))
            {
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "无效的 UI 显示层。");
            }

            if (!Enum.IsDefined(typeof(UIViewCacheMode), cacheMode))
            {
                throw new ArgumentOutOfRangeException(nameof(cacheMode), cacheMode, "无效的 UI 缓存策略。");
            }

            return new ViewOptions(
                assetPath,
                layer,
                cacheMode);
        }

        /// <summary>
        ///   <para>判断是否为页面类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsViewType(Type type) => type != null && typeof(ViewBase).IsAssignableFrom(type);

        /// <summary>
        ///   <para>校验打开模式。</para>
        /// </summary>
        /// <param name="openMode">打开模式。</param>
        private static bool ValidateOpenMode(UIViewOpenMode openMode)
        {
            if (!Enum.IsDefined(typeof(UIViewOpenMode), openMode))
            {
                throw new ArgumentOutOfRangeException(nameof(openMode), openMode, "无效的 UI 打开模式。");
            }

            return openMode == UIViewOpenMode.New;
        }

        /// <summary>
        ///   <para>校验页面类型。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        private static void ValidateViewType(Type viewType)
        {
            if (!IsViewType(viewType) || viewType.IsAbstract ||
                viewType.ContainsGenericParameters)
            {
                throw new ArgumentException("必须传入具体的 View 类型。", nameof(viewType));
            }
        }

        /// <summary>
        ///   <para>页面选项。</para>
        /// </summary>
        private readonly struct ViewOptions
        {
            /// <summary>
            ///   <para>资源路径。</para>
            /// </summary>
            internal readonly string AssetPath;
            /// <summary>
            ///   <para>层级。</para>
            /// </summary>
            internal readonly UILayer Layer;
            /// <summary>
            ///   <para>缓存模式。</para>
            /// </summary>
            internal readonly UIViewCacheMode CacheMode;

            /// <summary>
            ///   <para>创建页面选项。</para>
            /// </summary>
            /// <param name="assetPath">资源路径。</param>
            /// <param name="layer">层级。</param>
            /// <param name="cacheMode">关闭后的缓存模式。</param>
            internal ViewOptions(string assetPath, UILayer layer, UIViewCacheMode cacheMode)
            {
                AssetPath = assetPath;
                Layer = layer;
                CacheMode = cacheMode;
            }

            /// <summary>
            ///   <para>判断页面资源和显示策略是否相同。</para>
            /// </summary>
            /// <param name="other">待比较的配置。</param>
            internal bool Matches(ViewOptions other)
                => Game.PathUtility.ProjectPathComparer.Equals(AssetPath, other.AssetPath) &&
                   Layer == other.Layer && CacheMode == other.CacheMode;
        }

        /// <summary>
        ///   <para>页面条目。</para>
        /// </summary>
        private sealed class ViewEntry
        {
            /// <summary>
            ///   <para>实例标识。</para>
            /// </summary>
            internal readonly long InstanceId;
            /// <summary>
            ///   <para>页面类型。</para>
            /// </summary>
            internal readonly Type ViewType;
            /// <summary>
            ///   <para>页面。</para>
            /// </summary>
            internal readonly ViewBase View;
            /// <summary>
            ///   <para>页面节点；独立于绑定组件持有，确保组件被移除后仍可回收。</para>
            /// </summary>
            internal readonly GameObject Instance;
            /// <summary>
            ///   <para>句柄。</para>
            /// </summary>
            internal readonly AssetLoadHandle<GameObject> Handle;
            /// <summary>
            ///   <para>页面配置。</para>
            /// </summary>
            internal readonly ViewOptions Options;
            // 仅缓存页面使用；打开中的页面永远不会参与淘汰。
            /// <summary>
            ///   <para>最后访问顺序。</para>
            /// </summary>
            internal long LastAccessOrder;
            /// <summary>
            ///   <para>创建页面条目。</para>
            /// </summary>
            /// <param name="view">页面。</param>
            /// <param name="instance">页面节点。</param>
            /// <param name="handle">句柄。</param>
            /// <param name="options">页面配置。</param>
            /// <param name="instanceId">实例标识。</param>
            internal ViewEntry(
                ViewBase view,
                GameObject instance,
                AssetLoadHandle<GameObject> handle,
                ViewOptions options,
                long instanceId)
            {
                InstanceId = instanceId;
                ViewType = view.GetType();
                View = view;
                Instance = instance;
                Handle = handle;
                Options = options;
            }
        }

        /// <summary>
        ///   <para>打开操作；登记配置并持有加载完成结果。</para>
        /// </summary>
        private sealed class OpenOperation
        {
            /// <summary>
            ///   <para>页面类型。</para>
            /// </summary>
            internal readonly Type ViewType;
            /// <summary>
            ///   <para>页面配置。</para>
            /// </summary>
            internal readonly ViewOptions Options;
            /// <summary>
            ///   <para>父节点。</para>
            /// </summary>
            internal readonly Transform Parent;
            /// <summary>
            ///   <para>打开参数。</para>
            /// </summary>
            internal ViewArgs Args;
            /// <summary>
            ///   <para>是否创建独立实例。</para>
            /// </summary>
            internal readonly bool CreateNew;
            /// <summary>
            ///   <para>完成源；结束跟踪后通知等待者。</para>
            /// </summary>
            internal readonly TaskCompletionSource<ViewBase> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            /// <summary>
            ///   <para>创建打开操作。</para>
            /// </summary>
            /// <param name="viewType">页面类型。</param>
            /// <param name="options">页面配置。</param>
            /// <param name="parent">父节点。</param>
            /// <param name="args">打开参数。</param>
            /// <param name="createNew">是否创建独立实例。</param>
            internal OpenOperation(Type viewType, ViewOptions options, Transform parent, ViewArgs args, bool createNew)
            {
                ViewType = viewType;
                Options = options;
                Parent = parent;
                Args = args;
                CreateNew = createNew;
            }

            /// <summary>
            ///   <para>判断是否为可共享的打开请求。</para>
            /// </summary>
            /// <param name="options">页面配置。</param>
            /// <param name="parent">父节点。</param>
            /// <param name="args">打开参数。</param>
            internal bool Matches(ViewOptions options, Transform parent, ViewArgs args)
                => Options.Matches(options) && ReferenceEquals(Parent, parent) && ReferenceEquals(Args, args);
        }
    }
}

#endif
