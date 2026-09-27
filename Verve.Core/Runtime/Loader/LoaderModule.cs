#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;

    /// <summary>
    ///   <para>资源加载器模式。</para>
    /// </summary>
    public enum AssetLoaderMode : byte
    {
        /// <summary>
        ///   <para>使用 Unity <see cref="UnityEngine.AddressableAssets.Addressables"/>。</para>
        /// </summary>
        Addressables = 0,
        /// <summary>
        ///   <para>使用 <see cref="UnityEngine.AssetBundle"/> 加载资源。</para>
        /// </summary>
        AssetBundles = 1,
    }

    /// <summary>
    ///   <para>资源加载模块；安装前确定加载方式，对外使用统一项目路径。</para>
    /// </summary>
    [Serializable, GameModule("统一资源加载模块")]
    public sealed partial class LoaderModule : GameModule, ILoader
    {
        /// <summary>
        ///   <para>加载器模式。</para>
        /// </summary>
        [SerializeField, Tooltip("资源加载器模式。")] private AssetLoaderMode m_loaderMode = AssetLoaderMode.Addressables;

        /// <summary>
        ///   <para>当前资源加载实现。模块安装后不能修改，避免同一引用表中的资源由不同实现释放。</para>
        /// </summary>
        public AssetLoaderMode LoaderMode
        {
            get => m_loaderMode;
            set
            {
                ThrowIfLoaderConfigurationLocked(nameof(LoaderMode));
                if (!Enum.IsDefined(typeof(AssetLoaderMode), value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported asset loading mode.");
                }

                m_loaderMode = value;
            }
        }

        /// <summary>
        ///   <para>加载器配置已锁定时抛出异常。</para>
        /// </summary>
        /// <param name="propertyName">属性名称。</param>
        private void ThrowIfLoaderConfigurationLocked(string propertyName)
        {
            if (!IsInstalled)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{nameof(LoaderModule)}.{propertyName} cannot be changed while the module is installed.");
        }

        /// <summary>
        ///   <para>按规范化项目路径保存的资源引用；加载实现只负责创建和释放其底层资源。</para>
        /// </summary>
        private readonly Dictionary<string, AssetLoadReference> m_AssetReferences = new(Game.PathUtility.ProjectPathComparer);
        /// <summary>
        ///   <para>当前安装周期内异步加载操作的取消源；模块卸载时会取消仍在执行的异步加载。</para>
        /// </summary>
        private CancellationTokenSource m_LifecycleCancellation;

        /// <summary>
        ///   <para>当前加载器跟踪的资源数量。</para>
        /// </summary>
        private int AssetReferenceCount => m_AssetReferences.Count;

        /// <inheritdoc />
        public AssetLoadHandle<TObject> LoadAsset<TObject>(string assetPath)
            where TObject : UnityEngine.Object
        {
            assetPath = Game.PathUtility.NormalizeProjectPath(assetPath, nameof(assetPath));
            PrepareMainThreadOperation(nameof(LoadAsset));

            if (TryRetainAssetReference(assetPath, out TObject cachedAsset))
            {
                return CreateAssetLoadHandle(assetPath, cachedAsset);
            }

            var reference = CreateAssetReference<TObject>(assetPath);
            return CompleteAssetLoad<TObject>(assetPath, reference, default);
        }

        /// <inheritdoc />
        public async Task<AssetLoadHandle<TObject>> LoadAssetAsync<TObject>(string assetPath, CancellationToken ct = default)
            where TObject : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();
            assetPath = Game.PathUtility.NormalizeProjectPath(assetPath, nameof(assetPath));
            PrepareMainThreadOperation(nameof(LoadAssetAsync));

            if (TryRetainAssetReference(assetPath, out TObject cachedAsset))
            {
                return CreateAssetLoadHandle(assetPath, cachedAsset);
            }

            return await RunWithLifecycleCancellationAsync(ct, async operationToken =>
            {
                operationToken.ThrowIfCancellationRequested();
                var reference = await CreateAssetReferenceAsync<TObject>(assetPath, operationToken);
                return CompleteAssetLoad<TObject>(assetPath, reference, operationToken);
            });
        }

        /// <inheritdoc />
        public Task<SceneLoadHandle> LoadSceneAsync(
            string scenePath,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            scenePath = Game.PathUtility.NormalizeScenePath(scenePath, nameof(scenePath));
            PrepareMainThreadOperation(nameof(LoadSceneAsync));
            return LoadSceneAsyncImpl(scenePath, allowSceneActivation, parameters, onProgress);
        }

        /// <inheritdoc />
        public Task<SceneLoadHandle> UnloadSceneAsync(
            string scenePath,
            UnloadSceneOptions options = UnloadSceneOptions.None,
            Action<float> onProgress = null)
        {
            scenePath = Game.PathUtility.NormalizeScenePath(scenePath, nameof(scenePath));
            PrepareMainThreadOperation(nameof(UnloadSceneAsync));
            return UnloadSceneAsyncImpl(scenePath, options, onProgress);
        }

        /// <inheritdoc />
        protected sealed override async ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
        {
            Game.ThrowIfNotOnMainThread($"{GetType().Name}.Install");
            m_LifecycleCancellation = ct.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(ct)
                : new CancellationTokenSource();

            try
            {
                await InitializeLoadingModeAsync(ct);
            }
            catch (Exception installFailure)
            {
                var cleanupFailure = await ShutdownAndCleanupAsync();
                if (cleanupFailure != null)
                {
                    throw new AggregateException(
                        $"{GetType().Name} installation failed and cleanup also failed.",
                        installFailure,
                        cleanupFailure);
                }

                throw;
            }
        }

        /// <inheritdoc />
        protected sealed override async ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
        {
            Game.ThrowIfNotOnMainThread($"{GetType().Name}.Uninstall");
            ExceptionUtility.Rethrow(await ShutdownAndCleanupAsync());
        }

        /// <inheritdoc />
        protected sealed override void OnDispose()
        {
            Game.ThrowIfNotOnMainThread($"{GetType().Name}.Dispose");
            Exception failure = null;
            try { m_LifecycleCancellation?.Cancel(); }
            catch (Exception exception) { failure = exception; }
            try { DisposeLoadingMode(); }
            catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
            ExceptionUtility.Rethrow(CleanupAfterLifecycleExit(failure));
        }

        /// <summary>
        ///   <para>初始化安装前选定的资源加载实现。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private ValueTask InitializeLoadingModeAsync(CancellationToken ct)
        {
            return m_loaderMode switch
            {
                AssetLoaderMode.Addressables => InstallAddressablesAsync(ct),
                AssetLoaderMode.AssetBundles => InstallAssetBundlesAsync(ct),
                _ => throw new InvalidOperationException($"Unsupported asset loading mode: {m_loaderMode}."),
            };
        }

        /// <summary>
        ///   <para>回收当前资源加载实现持有的场景与状态。</para>
        /// </summary>
        private ValueTask ShutdownLoadingModeAsync()
        {
            return m_loaderMode switch
            {
                AssetLoaderMode.Addressables => UninstallAddressablesAsync(),
                AssetLoaderMode.AssetBundles => UninstallAssetBundlesAsync(),
                _ => default,
            };
        }

        /// <summary>
        ///   <para>释放当前资源加载实现的非资源引用状态。</para>
        /// </summary>
        private void DisposeLoadingMode()
        {
            switch (m_loaderMode)
            {
                case AssetLoaderMode.Addressables:
                    DisposeAddressables();
                    break;
                case AssetLoaderMode.AssetBundles:
                    DisposeAssetBundles();
                    break;
            }
        }

        /// <summary>
        ///   <para>按选定加载实现创建底层资源引用。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <typeparam name="TObject">对象类型。</typeparam>
        private AssetLoadReference CreateAssetReference<TObject>(string assetPath)
            where TObject : UnityEngine.Object
        {
            return m_loaderMode switch
            {
                AssetLoaderMode.Addressables => CreateAddressableAssetReference<TObject>(assetPath),
                AssetLoaderMode.AssetBundles => CreateAssetBundleAssetReference<TObject>(assetPath),
                _ => throw new InvalidOperationException($"Unsupported asset loading mode: {m_loaderMode}."),
            };
        }

        /// <summary>
        ///   <para>按选定加载实现异步创建底层资源引用。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="TObject">对象类型。</typeparam>
        private Task<AssetLoadReference> CreateAssetReferenceAsync<TObject>(
            string assetPath,
            CancellationToken ct)
            where TObject : UnityEngine.Object
        {
            return m_loaderMode switch
            {
                AssetLoaderMode.Addressables => CreateAddressableAssetReferenceAsync<TObject>(assetPath, ct),
                AssetLoaderMode.AssetBundles => CreateAssetBundleAssetReferenceAsync<TObject>(assetPath, ct),
                _ => Task.FromException<AssetLoadReference>(
                    new InvalidOperationException($"Unsupported asset loading mode: {m_loaderMode}.")),
            };
        }

        /// <summary>
        ///   <para>释放当前加载实现的附加资源缓存；公共资源引用由本模块统一释放。</para>
        /// </summary>
        private void ReleaseLoadingModeResources()
        {
            if (m_loaderMode == AssetLoaderMode.AssetBundles)
            {
                UnloadCachedBundles();
            }
        }

#if DEBUG || UNITY_EDITOR
        /// <summary>
        ///   <para>调试窗口显示的加载器摘要文本。</para>
        /// </summary>
        internal string DebugDescription => m_loaderMode switch
        {
            AssetLoaderMode.Addressables => GetAddressablesDebugDescription(),
            AssetLoaderMode.AssetBundles => GetAssetBundleDebugDescription(),
            _ => null,
        };

        /// <summary>
        ///   <para>复制当前加载器跟踪的资源调试快照。</para>
        /// </summary>
        /// <param name="output">快照输出列表。</param>
        internal void CopyDebugAssetSnapshotsTo(List<LoaderDebugAssetBuffer> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();

            foreach (var kv in m_AssetReferences)
            {
                var assetRef = kv.Value;
                var asset = assetRef.Asset;
                output.Add(new LoaderDebugAssetBuffer(
                    kv.Key,
                    asset == null ? "Destroyed" : asset.GetType().FullName,
                    assetRef.InstanceId,
                    assetRef.RefCount,
                    asset != null));
            }
        }

        /// <summary>
        ///   <para>复制当前加载器跟踪的 <see cref="UnityEngine.AssetBundle"/> 调试快照。</para>
        /// </summary>
        /// <param name="output">快照输出列表。</param>
        internal void CopyDebugBundleSnapshotsTo(List<LoaderDebugBundleBuffer> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (m_loaderMode == AssetLoaderMode.AssetBundles)
            {
                CopyAssetBundleDebugSnapshots(output);
                return;
            }

            output.Clear();
        }

        /// <summary>
        ///   <para>复制当前加载器跟踪的场景调试快照。</para>
        /// </summary>
        /// <param name="output">快照输出列表。</param>
        internal void CopyDebugSceneSnapshotsTo(List<LoaderDebugSceneBuffer> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (m_loaderMode == AssetLoaderMode.Addressables)
            {
                CopyAddressablesDebugSceneSnapshots(output);
                return;
            }

            output.Clear();
        }
#endif

        /// <summary>
        ///   <para>按选定加载实现加载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="allowSceneActivation">是否允许加载完成后自动激活场景。</param>
        /// <param name="parameters">场景加载参数。</param>
        /// <param name="onProgress">加载进度回调。</param>
        private Task<SceneLoadHandle> LoadSceneAsyncImpl(
            string scenePath,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            return m_loaderMode == AssetLoaderMode.Addressables
                ? LoadAddressableSceneAsync(scenePath, allowSceneActivation, parameters, onProgress)
                : LoadUnitySceneAsync(scenePath, allowSceneActivation, parameters, onProgress);
        }

        /// <summary>
        ///   <para>使用 Unity 场景管理器加载场景。资源包模式下场景需已纳入 Player 场景列表。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="allowSceneActivation">是否允许加载完成后自动激活场景。</param>
        /// <param name="parameters">场景加载参数。</param>
        /// <param name="onProgress">进度回调。</param>
        private Task<SceneLoadHandle> LoadUnitySceneAsync(
            string scenePath,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            var operation = SceneManager.LoadSceneAsync(scenePath, parameters);
            if (operation == null)
            {
                throw new InvalidOperationException($"Failed to start scene load. path={scenePath}");
            }

            operation.allowSceneActivation = allowSceneActivation;
            return WaitForSceneOperationAsync(operation, allowSceneActivation, onProgress, scenePath);
        }

        /// <summary>
        ///   <para>按选定加载实现卸载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="options">场景卸载参数。</param>
        /// <param name="onProgress">卸载进度回调。</param>
        private Task<SceneLoadHandle> UnloadSceneAsyncImpl(
            string scenePath,
            UnloadSceneOptions options = UnloadSceneOptions.None,
            Action<float> onProgress = null)
        {
            return m_loaderMode == AssetLoaderMode.Addressables
                ? UnloadAddressableSceneAsync(scenePath, options, onProgress)
                : UnloadUnitySceneAsync(scenePath, options, onProgress);
        }

        /// <summary>
        ///   <para>使用 Unity 场景管理器卸载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="options">选项。</param>
        /// <param name="onProgress">进度回调。</param>
        private Task<SceneLoadHandle> UnloadUnitySceneAsync(
            string scenePath,
            UnloadSceneOptions options = UnloadSceneOptions.None,
            Action<float> onProgress = null)
        {
            var scene = GetLoadedScene(scenePath);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException($"Scene is not loaded or cannot be found. path={scenePath}");
            }

            var operation = SceneManager.UnloadSceneAsync(scene, options);
            if (operation == null)
            {
                throw new InvalidOperationException($"Failed to start scene unload. path={scenePath}");
            }

            return WaitForSceneOperationAsync(operation, true, onProgress, scenePath, scene);
        }

        /// <summary>
        ///   <para>执行加载器公共入口的通用校验。</para>
        /// </summary>
        /// <param name="operationName">操作名称。</param>
        private void PrepareMainThreadOperation(string operationName)
        {
            ThrowIfLoaderUnavailable();
            Game.ThrowIfNotOnMainThread($"{GetType().Name}.{operationName}");
        }

        /// <summary>
        ///   <para>执行与加载器安装周期绑定的异步操作。</para>
        /// </summary>
        /// <param name="ct">外部取消令牌。</param>
        /// <param name="operation">实际异步操作。</param>
        private async Task RunWithLifecycleCancellationAsync(
            CancellationToken ct,
            Func<CancellationToken, Task> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var cancellationSource = CreateLinkedLifecycleCancellation(ct, out var operationToken);
            try
            {
                operationToken.ThrowIfCancellationRequested();
                await operation(operationToken);
            }
            finally
            {
                cancellationSource?.Dispose();
            }
        }

        /// <summary>
        ///   <para>执行与加载器安装周期绑定的异步操作。</para>
        /// </summary>
        /// <typeparam name="TResult">异步操作结果类型。</typeparam>
        /// <param name="ct">外部取消令牌。</param>
        /// <param name="operation">实际异步操作。</param>
        private async Task<TResult> RunWithLifecycleCancellationAsync<TResult>(
            CancellationToken ct,
            Func<CancellationToken, Task<TResult>> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var cancellationSource = CreateLinkedLifecycleCancellation(ct, out var operationToken);
            try
            {
                operationToken.ThrowIfCancellationRequested();
                return await operation(operationToken);
            }
            finally
            {
                cancellationSource?.Dispose();
            }
        }

        /// <summary>
        ///   <para>把 Unity 资源实例转换为调用方请求的类型。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="asset">Unity 资源实例。</param>
        /// <param name="assetPath">资源路径，用于错误信息。</param>
        private static TObject CastAsset<TObject>(UnityEngine.Object asset, string assetPath)
            where TObject : UnityEngine.Object
        {
            if (asset == null)
            {
                return default;
            }

            if (asset is TObject typedAsset)
            {
                return typedAsset;
            }

            throw new InvalidCastException(
                $"Asset type mismatch. path={assetPath}, expected={typeof(TObject).FullName}, actual={asset.GetType().FullName}");
        }

        /// <summary>
        ///   <para>等待不可直接取消的 Unity 异步操作完成，并返回等待期间是否收到取消请求。</para>
        /// </summary>
        /// <remarks>
        ///   <para>Unity 资源加载操作通常不能真正取消。收到取消请求后仍等待完成，让调用方有机会释放刚完成的底层资源。</para>
        /// </remarks>
        /// <param name="operation">Unity 异步操作。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="onProgress">进度回调。</param>
        private static async Task<bool> WaitForUnityOperationAsync(
            AsyncOperation operation,
            CancellationToken ct,
            Action<float> onProgress = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            var cancellationRequested = false;
            while (!operation.isDone)
            {
                if (!cancellationRequested && ct.IsCancellationRequested)
                {
                    cancellationRequested = true;
                }

                if (!cancellationRequested)
                {
                    onProgress?.Invoke(Mathf.Clamp01(operation.progress));
                }

                await Task.Yield();
            }

            cancellationRequested |= ct.IsCancellationRequested;
            if (!cancellationRequested)
            {
                onProgress?.Invoke(1f);
            }

            return cancellationRequested;
        }

        /// <summary>
        ///   <para>尝试保留资源引用。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="asset">资源。</param>
        /// <typeparam name="TObject">对象类型。</typeparam>
        private bool TryRetainAssetReference<TObject>(string assetPath, out TObject asset)
            where TObject : UnityEngine.Object
        {
            asset = default;
            if (!m_AssetReferences.TryGetValue(assetPath, out var assetRef))
            {
                return false;
            }

            if (assetRef.Asset == null)
            {
                RemoveAssetReference(assetPath, assetRef);
                return false;
            }

            asset = CastAsset<TObject>(assetRef.Asset, assetPath);
            assetRef.Retain();
            return true;
        }

        /// <summary>
        ///   <para>完成资源加载。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="reference">引用。</param>
        /// <param name="ct">取消令牌。</param>
        /// <typeparam name="TObject">对象类型。</typeparam>
        private AssetLoadHandle<TObject> CompleteAssetLoad<TObject>(
            string assetPath,
            AssetLoadReference reference,
            CancellationToken ct)
            where TObject : UnityEngine.Object
        {
            if (reference == null)
            {
                throw new InvalidOperationException($"{GetType().Name} returned a null asset reference. path={assetPath}");
            }

            var releaseReference = true;
            AssetLoadHandle<TObject> handle = null;
            Exception failure = null;
            try
            {
                if (ct.IsCancellationRequested || !IsInstalled)
                {
                    ct.ThrowIfCancellationRequested();
                    ThrowIfLoaderUnavailable();
                }

                if (TryRetainAssetReference(assetPath, out TObject cachedAsset))
                {
                    handle = CreateAssetLoadHandle(assetPath, cachedAsset);
                }
                else
                {
                    var asset = CastAsset<TObject>(reference.Asset, assetPath);
                    if (asset == null)
                        throw new KeyNotFoundException($"Asset cannot be loaded. path={assetPath}, type={typeof(TObject).FullName}");

                    m_AssetReferences.Add(assetPath, reference);
                    releaseReference = false;
                    handle = CreateAssetLoadHandle(assetPath, asset);
                }
            }
            catch (Exception exception) { failure = exception; }

            if (releaseReference)
            {
                try { reference.ReleaseResources(); }
                catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
            }

            if (failure != null)
            {
                // 句柄尚未交给调用方，失败时归还已保留的缓存引用。
                try { handle?.Dispose(); }
                catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
                ExceptionUtility.Rethrow(failure);
            }
            return handle;
        }

        /// <summary>
        ///   <para>校验加载器是否处于已安装可用状态。</para>
        /// </summary>
        private void ThrowIfLoaderUnavailable()
        {
            if (!IsInstalled)
            {
                throw new InvalidOperationException($"{GetType().Name} is not installed.");
            }
        }

        /// <summary>
        ///   <para>创建关联生命周期的取消源。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        /// <param name="operationToken">取消令牌。</param>
        private CancellationTokenSource CreateLinkedLifecycleCancellation(
            CancellationToken ct,
            out CancellationToken operationToken)
        {
            var lifecycleCancellation = m_LifecycleCancellation;
            if (lifecycleCancellation == null)
            {
                operationToken = ct;
                return null;
            }

            var lifecycleToken = lifecycleCancellation.Token;
            if (!ct.CanBeCanceled)
            {
                operationToken = lifecycleToken;
                return null;
            }

            var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct, lifecycleToken);
            operationToken = linkedCancellation.Token;
            return linkedCancellation;
        }

        /// <summary>
        ///   <para>根据资源实例创建可释放句柄。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="asset">资源实例。</param>
        private AssetLoadHandle<TObject> CreateAssetLoadHandle<TObject>(string assetPath, TObject asset)
            where TObject : UnityEngine.Object
        {
            if (asset == null)
            {
                return new AssetLoadHandle<TObject>(asset);
            }

            var reference = m_AssetReferences[assetPath];
            return new AssetLoadHandle<TObject>(
                asset,
                () => ReleaseAssetReference(assetPath, reference));
        }

        /// <summary>
        ///   <para>释放由 <see cref="AssetLoadHandle{T}"/> 管理的一次资源引用。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="reference">加载时的引用记录；旧句柄不能释放后来重新加载的同路径资源。</param>
        private void ReleaseAssetReference(string assetPath, AssetLoadReference reference)
        {
            if (!IsInstalled)
            {
                return;
            }

            Game.ThrowIfNotOnMainThread($"{GetType().Name}.{nameof(AssetLoadHandle<UnityEngine.Object>.Dispose)}");
            if (string.IsNullOrWhiteSpace(assetPath)) return;
            if (!m_AssetReferences.TryGetValue(assetPath, out var assetRef)) return;
            if (!ReferenceEquals(assetRef, reference)) return;

            if (assetRef.Release() > 0) return;
            RemoveAssetReference(assetPath, assetRef);
        }

        /// <summary>
        ///   <para>释放已登记资源。</para>
        /// </summary>
        private void ReleaseTrackedResources()
        {
            List<Exception> errors = null;

            try
            {
                ReleaseTrackedAssetReferences();
            }
            catch (Exception ex)
            {
                ExceptionUtility.Add(ref errors, ex);
            }

            try
            {
                ReleaseLoadingModeResources();
            }
            catch (Exception ex)
            {
                ExceptionUtility.Add(ref errors, ex);
            }

            ExceptionUtility.ThrowIfAny(errors);
        }

        /// <summary>
        ///   <para>释放已登记资源引用。</para>
        /// </summary>
        private void ReleaseTrackedAssetReferences()
        {
            if (m_AssetReferences.Count == 0)
            {
                return;
            }

            var assetRefs = new List<AssetLoadReference>(m_AssetReferences.Values);
            m_AssetReferences.Clear();
            ResourceUtility.ReleaseAll(assetRefs, reference => reference.ReleaseResources());
        }

        /// <summary>
        ///   <para>移除资源引用。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="assetRef">资源引用。</param>
        private void RemoveAssetReference(string assetPath, AssetLoadReference assetRef)
        {
            m_AssetReferences.Remove(assetPath);
            assetRef.ReleaseResources();
        }

        /// <summary>
        ///   <para>停止加载并清理资源；返回全部清理错误。</para>
        /// </summary>
        private async ValueTask<Exception> ShutdownAndCleanupAsync()
        {
            Exception failure = null;
            try { m_LifecycleCancellation?.Cancel(); }
            catch (Exception exception) { failure = exception; }
            try { await ShutdownLoadingModeAsync(); }
            catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
            return CleanupAfterLifecycleExit(failure);
        }

        /// <summary>
        ///   <para>清理生命周期退出后遗留的资源。</para>
        /// </summary>
        /// <param name="failure">已有错误。</param>
        private Exception CleanupAfterLifecycleExit(Exception failure)
        {
            try { ReleaseTrackedResources(); }
            catch (Exception exception) { failure = ExceptionUtility.Combine(failure, exception); }
            var cancellation = m_LifecycleCancellation;
            m_LifecycleCancellation = null;
            cancellation?.Dispose();
            return failure;
        }

        /// <summary>
        ///   <para>异步等待场景操作。</para>
        /// </summary>
        /// <param name="operation">操作。</param>
        /// <param name="allowSceneActivation">是否允许加载完成后自动激活场景。</param>
        /// <param name="onProgress">进度回调。</param>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="scene">操作关联的场景。</param>
        private static async Task<SceneLoadHandle> WaitForSceneOperationAsync(
            AsyncOperation operation,
            bool allowSceneActivation,
            Action<float> onProgress,
            string scenePath,
            Scene scene = default)
        {
            while (!operation.isDone)
            {
                var progress = allowSceneActivation
                    ? Mathf.Clamp01(operation.progress)
                    : Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(progress);

                if (!allowSceneActivation && operation.progress >= 0.9f)
                {
                    break;
                }

                await Task.Yield();
            }

            onProgress?.Invoke(allowSceneActivation ? 1f : Mathf.Clamp01(operation.progress / 0.9f));
            return scene.IsValid()
                ? new SceneLoadHandle(operation, scene)
                : new SceneLoadHandle(operation, GetLoadedScene(scenePath));
        }

        /// <summary>
        ///   <para>获取已加载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        private static Scene GetLoadedScene(string scenePath) => SceneManager.GetSceneByPath(scenePath);


    }
}

#endif
