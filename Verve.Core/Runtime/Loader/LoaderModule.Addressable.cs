#if UNITY_2018_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    
    /// <summary>
    ///   <para>资源加载模块；实现 <see cref="UnityEngine.AddressableAssets.Addressables"/> 加载，地址须与项目路径一致。</para>
    /// </summary>
    public sealed partial class LoaderModule
    {
        /// <summary>
        ///   <para>按规范化场景路径缓存的场景引用。</para>
        /// </summary>
        private readonly Dictionary<string, SceneReference> m_LoadedScenes = new(Game.PathUtility.ProjectPathComparer);

        /// <summary>
        ///   <para><see cref="UnityEngine.AddressableAssets.Addressables"/> 已初始化。</para>
        /// </summary>
        private bool m_AddressablesInitialized;

        /// <summary>
        ///   <para>异步安装 <see cref="UnityEngine.AddressableAssets.Addressables"/>。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async ValueTask InstallAddressablesAsync(CancellationToken ct) => await RunWithLifecycleCancellationAsync(ct, InitializeAddressablesAsync);

        /// <summary>
        ///   <para>异步卸载 <see cref="UnityEngine.AddressableAssets.Addressables"/>。</para>
        /// </summary>
        private async ValueTask UninstallAddressablesAsync() => await UnloadAllTrackedScenesAsync();

        /// <summary>
        ///   <para>释放 <see cref="UnityEngine.AddressableAssets.Addressables"/>。</para>
        /// </summary>
        private void DisposeAddressables() => ReleaseAllTrackedScenes();

        /// <summary>
        ///   <para>同步加载 <see cref="UnityEngine.AddressableAssets.Addressables"/> 资源。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">资源项目路径。</param>
        private AssetLoadReference CreateAddressableAssetReference<TObject>(string assetPath)
            where TObject : UnityEngine.Object
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException("WebGL Addressables require LoadAssetAsync.");
#else
            var handle = Addressables.LoadAssetAsync<TObject>(assetPath);
            try
            {
                var asset = handle.WaitForCompletion();
                ThrowIfOperationFailed(handle, "load Addressables asset", assetPath);
                if (asset == null)
                {
                    throw new KeyNotFoundException($"Addressables asset cannot be loaded. path={assetPath}, type={typeof(TObject).FullName}");
                }

                return new AddressableAssetReference(handle, asset);
            }
            catch
            {
                ReleaseAddressablesHandle(handle);
                throw;
            }
#endif
        }

        /// <summary>
        ///   <para>异步加载 <see cref="UnityEngine.AddressableAssets.Addressables"/> 资源。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">资源项目路径。</param>
        /// <param name="ct">取消令牌。</param>
        private async Task<AssetLoadReference> CreateAddressableAssetReferenceAsync<TObject>(
            string assetPath,
            CancellationToken ct)
            where TObject : UnityEngine.Object
        {
            ct.ThrowIfCancellationRequested();

            var handle = Addressables.LoadAssetAsync<TObject>(assetPath);
            try
            {
                var asset = await WaitForOperationAsync(handle, "load Addressables asset", assetPath, ct);
                if (asset == null)
                {
                    throw new KeyNotFoundException($"Addressables asset cannot be loaded. path={assetPath}, type={typeof(TObject).FullName}");
                }

                return new AddressableAssetReference(handle, asset);
            }
            catch
            {
                ReleaseAddressablesHandle(handle);
                throw;
            }
        }

        /// <summary>
        ///   <para>异步加载 <see cref="UnityEngine.AddressableAssets.Addressables"/> 场景。</para>
        /// </summary>
        /// <param name="scenePath">场景项目路径。</param>
        /// <param name="allowSceneActivation">是否允许加载完成后自动激活场景。</param>
        /// <param name="parameters">场景加载参数。</param>
        /// <param name="onProgress">加载进度回调。</param>
        private async Task<SceneLoadHandle> LoadAddressableSceneAsync(
            string scenePath,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            RemoveInvalidSceneRefs();

            if (TryRetainLoadedScene(scenePath, out var retainedHandle))
            {
                return retainedHandle;
            }

            var handle = Addressables.LoadSceneAsync(
                scenePath,
                parameters,
                allowSceneActivation);
            try
            {
                return await RunWithLifecycleCancellationAsync(default, async operationToken =>
                {
                    var sceneInstance = await WaitForOperationAsync(
                        handle,
                        "load Addressables scene",
                        scenePath,
                        operationToken,
                        onProgress);
                    return TrackLoadedScene(scenePath, handle, sceneInstance);
                });
            }
            catch
            {
                ReleaseAddressablesHandle(handle);
                throw;
            }
        }

        /// <summary>
        ///   <para>异步卸载 <see cref="UnityEngine.AddressableAssets.Addressables"/> 场景。</para>
        /// </summary>
        /// <param name="scenePath">场景项目路径。</param>
        /// <param name="options">场景卸载参数。</param>
        /// <param name="onProgress">卸载进度回调。</param>
        private async Task<SceneLoadHandle> UnloadAddressableSceneAsync(
            string scenePath,
            UnloadSceneOptions options = UnloadSceneOptions.None,
            Action<float> onProgress = null)
        {
            RemoveInvalidSceneRefs();

            if (!TryGetLoadedScene(scenePath, out var trackedScenePath, out var sceneRef))
            {
                throw new InvalidOperationException($"Addressables scene is not tracked by this loader. path={scenePath}");
            }

            if (!sceneRef.IsHandleValid)
            {
                m_LoadedScenes.Remove(trackedScenePath);
                throw new InvalidOperationException($"Tracked Addressables scene handle is no longer valid. path={scenePath}");
            }

            if (sceneRef.RefCount > 1)
            {
                sceneRef.Release();
                return sceneRef.CreateHandle();
            }

            try
            {
                var result = await UnloadSceneRefAsync(trackedScenePath, sceneRef, options, onProgress);
                m_LoadedScenes.Remove(trackedScenePath);
                return result;
            }
            catch
            {
                if (!sceneRef.IsHandleValid)
                {
                    m_LoadedScenes.Remove(trackedScenePath);
                }

                throw;
            }
        }

#if DEBUG || UNITY_EDITOR
        /// <summary>
        ///   <para><see cref="UnityEngine.AddressableAssets.Addressables"/> 加载器调试描述。</para>
        /// </summary>
        private string GetAddressablesDebugDescription()
        {
            RemoveInvalidSceneRefs();
            return $"Assets: {AssetReferenceCount}, Scenes: {m_LoadedScenes.Count}";
        }

        /// <summary>
        ///   <para>复制当前 <see cref="UnityEngine.AddressableAssets.Addressables"/> 场景快照。</para>
        /// </summary>
        /// <param name="output">输出列表。</param>
        private void CopyAddressablesDebugSceneSnapshots(List<LoaderDebugSceneBuffer> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            RemoveInvalidSceneRefs();

            foreach (var kv in m_LoadedScenes)
            {
                var sceneRef = kv.Value;
                var scene = sceneRef.Scene;
                var valid = scene.IsValid();
                output.Add(new LoaderDebugSceneBuffer(
                    kv.Key,
                    valid ? scene.name : "Invalid",
                    valid ? scene.path : null,
                    valid ? scene.buildIndex : -1,
                    sceneRef.RefCount,
                    valid,
                    valid && scene.isLoaded));
            }
        }
#endif

        /// <summary>
        ///   <para>异步初始化 <see cref="UnityEngine.AddressableAssets.Addressables"/>。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async Task InitializeAddressablesAsync(CancellationToken ct)
        {
            if (m_AddressablesInitialized) return;

            var handle = Addressables.InitializeAsync(false);
            try
            {
                await WaitForOperationAsync(handle, "initialize Addressables", null, ct);
                m_AddressablesInitialized = true;
            }
            finally
            {
                ReleaseAddressablesHandle(handle);
            }
        }

        /// <summary>
        ///   <para>登记已加载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="handle">句柄。</param>
        /// <param name="sceneInstance">场景实例。</param>
        private SceneLoadHandle TrackLoadedScene(
            string scenePath,
            AsyncOperationHandle<SceneInstance> handle,
            SceneInstance sceneInstance)
        {
            if (!sceneInstance.Scene.IsValid())
            {
                throw new InvalidOperationException($"Addressables scene loaded without a valid Scene instance. path={scenePath}");
            }

            if (TryRetainLoadedScene(scenePath, out var retainedHandle))
            {
                StartAddressablesSceneUnload(handle, UnloadSceneOptions.None);
                return retainedHandle;
            }

            var sceneRef = new SceneReference(handle);
            m_LoadedScenes.Add(scenePath, sceneRef);
            return sceneRef.CreateHandle();
        }

        /// <summary>
        ///   <para>尝试保留已加载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="handle">句柄。</param>
        private bool TryRetainLoadedScene(string scenePath, out SceneLoadHandle handle)
        {
            handle = default;
            if (!TryGetLoadedScene(scenePath, out var trackedScenePath, out var sceneRef))
            {
                return false;
            }

            if (!sceneRef.IsHandleValid)
            {
                m_LoadedScenes.Remove(trackedScenePath);
                return false;
            }

            sceneRef.Retain();
            handle = sceneRef.CreateHandle();
            return true;
        }

        /// <summary>
        ///   <para>尝试获取已加载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="trackedScenePath">已登记场景路径。</param>
        /// <param name="sceneRef">场景引用。</param>
        private bool TryGetLoadedScene(string scenePath, out string trackedScenePath, out SceneReference sceneRef)
        {
            if (m_LoadedScenes.TryGetValue(scenePath, out sceneRef))
            {
                trackedScenePath = scenePath;
                return true;
            }

            trackedScenePath = null;
            sceneRef = null;
            return false;
        }

        /// <summary>
        ///   <para>异步卸载场景引用。</para>
        /// </summary>
        /// <param name="scenePath">场景路径。</param>
        /// <param name="sceneRef">场景引用。</param>
        /// <param name="options">选项。</param>
        /// <param name="onProgress">进度回调。</param>
        private async Task<SceneLoadHandle> UnloadSceneRefAsync(
            string scenePath,
            SceneReference sceneRef,
            UnloadSceneOptions options,
            Action<float> onProgress)
        {
            var scene = sceneRef.Scene;
            if (!sceneRef.IsHandleValid)
            {
                return new SceneLoadHandle(scene);
            }

            var unloadHandle = Addressables.UnloadSceneAsync(sceneRef.Handle, options, false);
            try
            {
                await WaitForOperationAsync(unloadHandle, "unload Addressables scene", scenePath, default, onProgress);
                return new SceneLoadHandle(scene);
            }
            finally
            {
                ReleaseAddressablesHandle(unloadHandle);
            }
        }

        /// <summary>
        ///   <para>异步卸载全部已登记场景。</para>
        /// </summary>
        private async Task UnloadAllTrackedScenesAsync()
        {
            RemoveInvalidSceneRefs();
            if (m_LoadedScenes.Count == 0)
            {
                return;
            }

            var scenes = new List<KeyValuePair<string, SceneReference>>(m_LoadedScenes);
            List<Exception> errors = null;
            for (int i = scenes.Count - 1; i >= 0; i--)
            {
                var scene = scenes[i];
                try
                {
                    await UnloadSceneRefAsync(scene.Key, scene.Value, UnloadSceneOptions.None, null);
                }
                catch (Exception ex)
                {
                    ExceptionUtility.Add(ref errors, ex);
                    try
                    {
                        StartAddressablesSceneUnload(scene.Value.Handle, UnloadSceneOptions.None);
                    }
                    catch (Exception releaseException)
                    {
                        ExceptionUtility.Add(ref errors, releaseException);
                    }
                }
                finally
                {
                    m_LoadedScenes.Remove(scene.Key);
                }
            }

            ThrowSceneUnloadErrors(errors);
        }

        /// <summary>
        ///   <para>释放全部已登记场景。</para>
        /// </summary>
        private void ReleaseAllTrackedScenes()
        {
            RemoveInvalidSceneRefs();
            if (m_LoadedScenes.Count == 0)
            {
                return;
            }

            List<Exception> errors = null;
            foreach (var sceneRef in m_LoadedScenes.Values)
            {
                try
                {
                    StartAddressablesSceneUnload(sceneRef.Handle, UnloadSceneOptions.None);
                }
                catch (Exception ex)
                {
                    ExceptionUtility.Add(ref errors, ex);
                }
            }

            m_LoadedScenes.Clear();
            ThrowSceneUnloadErrors(errors);
        }

        /// <summary>
        ///   <para>移除无效场景引用。</para>
        /// </summary>
        private void RemoveInvalidSceneRefs()
        {
            if (m_LoadedScenes.Count == 0)
            {
                return;
            }

            List<string> invalidSceneKeys = null;
            foreach (var kv in m_LoadedScenes)
            {
                if (kv.Value.IsHandleValid)
                {
                    continue;
                }

                invalidSceneKeys ??= new List<string>();
                invalidSceneKeys.Add(kv.Key);
            }

            if (invalidSceneKeys == null)
            {
                return;
            }

            for (int i = 0; i < invalidSceneKeys.Count; i++)
            {
                m_LoadedScenes.Remove(invalidSceneKeys[i]);
            }
        }

        /// <summary>
        ///   <para>异步等待操作。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="operationName">操作名称。</param>
        /// <param name="path">路径。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="onProgress">进度回调。</param>
        /// <typeparam name="TObject">对象类型。</typeparam>
        private static async Task<TObject> WaitForOperationAsync<TObject>(
            AsyncOperationHandle<TObject> handle,
            string operationName,
            string path,
            CancellationToken ct,
            Action<float> onProgress = null)
        {
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }

            ct.ThrowIfCancellationRequested();
            ThrowIfOperationFailed(handle, operationName, path);
            onProgress?.Invoke(1f);
            return handle.Result;
        }

        /// <summary>
        ///   <para>异步等待操作。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="operationName">操作名称。</param>
        /// <param name="path">路径。</param>
        /// <param name="ct">取消令牌。</param>
        /// <param name="onProgress">进度回调。</param>
        private static async Task WaitForOperationAsync(
            AsyncOperationHandle handle,
            string operationName,
            string path,
            CancellationToken ct,
            Action<float> onProgress = null)
        {
            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }

            ct.ThrowIfCancellationRequested();
            ThrowIfOperationFailed(handle, operationName, path);
            onProgress?.Invoke(1f);
        }

        /// <summary>
        ///   <para>操作失败时抛出异常。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="operationName">操作名称。</param>
        /// <param name="path">路径。</param>
        /// <typeparam name="TObject">对象类型。</typeparam>
        private static void ThrowIfOperationFailed<TObject>(
            AsyncOperationHandle<TObject> handle,
            string operationName,
            string path)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            throw CreateOperationFailure(handle.OperationException, operationName, path);
        }

        /// <summary>
        ///   <para>操作失败时抛出异常。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="operationName">操作名称。</param>
        /// <param name="path">路径。</param>
        private static void ThrowIfOperationFailed(
            AsyncOperationHandle handle,
            string operationName,
            string path)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return;
            }

            throw CreateOperationFailure(handle.OperationException, operationName, path);
        }

        /// <summary>
        ///   <para>创建加载失败异常。</para>
        /// </summary>
        /// <param name="exception">异常。</param>
        /// <param name="operationName">操作名称。</param>
        /// <param name="path">路径。</param>
        private static InvalidOperationException CreateOperationFailure(
            Exception exception,
            string operationName,
            string path)
        {
            var pathText = string.IsNullOrEmpty(path) ? "" : $", path={path}";
            return new InvalidOperationException($"Failed to {operationName}{pathText}.", exception);
        }

        /// <summary>
        ///   <para>释放 <see cref="UnityEngine.AddressableAssets.Addressables"/> 句柄。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        private static void ReleaseAddressablesHandle(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        /// <summary>
        ///   <para>开始 <see cref="UnityEngine.AddressableAssets.Addressables"/> 场景卸载。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        /// <param name="options">选项。</param>
        private static void StartAddressablesSceneUnload(
            AsyncOperationHandle<SceneInstance> handle,
            UnloadSceneOptions options)
        {
            if (!handle.IsValid())
            {
                return;
            }

            Addressables.UnloadSceneAsync(handle, options, true);
        }

        /// <summary>
        ///   <para>抛出场景卸载错误。</para>
        /// </summary>
        /// <param name="errors">错误。</param>
        private static void ThrowSceneUnloadErrors(List<Exception> errors)
        {
            if (errors == null || errors.Count == 0)
            {
                return;
            }

            if (errors.Count == 1)
            {
                throw new InvalidOperationException("Failed to unload tracked Addressables scenes.", errors[0]);
            }

            throw new AggregateException("Failed to unload tracked Addressables scenes.", errors);
        }

        /// <summary>
        ///   <para><see cref="UnityEngine.AddressableAssets.Addressables"/> 资源引用。</para>
        /// </summary>
        private sealed class AddressableAssetReference : AssetLoadReference
        {
            /// <summary>
            ///   <para>已释放。</para>
            /// </summary>
            private bool m_Released;

            /// <summary>
            ///   <para><see cref="UnityEngine.AddressableAssets.Addressables"/> 操作句柄。</para>
            /// </summary>
            public AsyncOperationHandle Handle { get; }

            /// <summary>
            ///   <para>创建 <see cref="UnityEngine.AddressableAssets.Addressables"/> 资源引用。</para>
            /// </summary>
            /// <param name="handle">句柄。</param>
            /// <param name="asset">资源。</param>
            public AddressableAssetReference(AsyncOperationHandle handle, UnityEngine.Object asset)
                : base(asset) => Handle = handle;

            /// <inheritdoc />
            public override void ReleaseResources()
            {
                if (m_Released)
                {
                    return;
                }

                m_Released = true;
                ReleaseAddressablesHandle(Handle);
            }
        }

        /// <summary>
        ///   <para>场景引用。</para>
        /// </summary>
        private sealed class SceneReference : LoadReference
        {
            /// <summary>
            ///   <para><see cref="UnityEngine.AddressableAssets.Addressables"/> 场景加载句柄。</para>
            /// </summary>
            public AsyncOperationHandle<SceneInstance> Handle { get; }

            /// <summary>
            ///   <para>场景加载句柄是否仍有效。</para>
            /// </summary>
            public bool IsHandleValid => Handle.IsValid();

            /// <summary>
            ///   <para>当前场景实例。</para>
            /// </summary>
            public Scene Scene => Handle.IsValid() ? Handle.Result.Scene : default;

            /// <summary>
            ///   <para>创建场景引用。</para>
            /// </summary>
            /// <param name="handle">句柄。</param>
            public SceneReference(AsyncOperationHandle<SceneInstance> handle) => Handle = handle;

            /// <summary>
            ///   <para>创建句柄。</para>
            /// </summary>
            public SceneLoadHandle CreateHandle() => new SceneLoadHandle(Scene, Activate);

            /// <summary>
            ///   <para>激活。</para>
            /// </summary>
            private void Activate()
            {
                if (Handle.IsValid())
                {
                    Handle.Result.ActivateAsync();
                }
            }
        }
    }
}

#endif
