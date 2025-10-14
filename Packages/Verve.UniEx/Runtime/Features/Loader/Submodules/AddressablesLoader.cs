#if UNITY_2018_3_OR_NEWER
    
namespace Verve.UniEx.Loader
{
    using System;
    using System.Linq;
    using UnityEngine;
    using Verve.Loader;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;


    /// <summary>
    /// 可寻址资源加载器
    /// </summary>
    [System.Serializable, GameFeatureSubmodule(typeof(LoaderGameFeature), Description = "可寻址资源加载器")]
    public sealed partial class AddressablesLoader : LoaderSubmodule, IHotUpdateUpdater
    {
        private readonly Dictionary<string, AsyncOperationHandle> m_AssetHandles = new Dictionary<string, AsyncOperationHandle>();
        private readonly Dictionary<string, AsyncOperationHandle> m_SceneHandles = new Dictionary<string, AsyncOperationHandle>();


        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> CheckForUpdatesAsync(CancellationToken ct = default)
        {
            var handle = Addressables.CheckForCatalogUpdates(false);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            
            Addressables.Release(handle);
            return null;
        }

        /// <summary>
        /// 应用更新
        /// </summary>
        /// <param name="catalogs">要更新的目录列表</param>
        /// <param name="onProgress">进度回调函数，参数为进度百分比</param>
        /// <param name="ct">取消令牌</param>
        public async Task ApplyUpdatesAsync(IEnumerable<string> catalogs, Action<float> onProgress = null, CancellationToken ct = default)
        {
            if (catalogs == null || !catalogs.Any()) 
            {
                return;
            }
            
            var updateHandle = Addressables.UpdateCatalogs(catalogs);
            while (!updateHandle.IsDone && !ct.IsCancellationRequested)
            {
                onProgress?.Invoke(updateHandle.PercentComplete * 0.3f);
                await Task.Yield();
            }
            
            if (ct.IsCancellationRequested || updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(updateHandle);
                onProgress?.Invoke(0f);
                return;
            }
            
            var resourceLocators = updateHandle.Result;
            var updateKeys = resourceLocators.SelectMany(x => x.Keys).Distinct();
            
            var sizeHandle = Addressables.GetDownloadSizeAsync(updateKeys);
            while (!sizeHandle.IsDone && !ct.IsCancellationRequested)
            {
                onProgress?.Invoke(0.3f + sizeHandle.PercentComplete * 0.1f);
                await Task.Yield();
            }
            
            if (ct.IsCancellationRequested)
            {
                Addressables.Release(sizeHandle);
                Addressables.Release(updateHandle);
                onProgress?.Invoke(0f);
                return;
            }
            
            var downloadSize = await sizeHandle.Task;
            Addressables.Release(sizeHandle);
            
            if (downloadSize > 0)
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(updateKeys, Addressables.MergeMode.Union);
                
                while (!downloadHandle.IsDone && !ct.IsCancellationRequested)
                {
                    onProgress?.Invoke(0.4f + downloadHandle.PercentComplete * 0.6f);
                    await Task.Yield();
                }
                
                if (ct.IsCancellationRequested)
                {
                    Addressables.Release(downloadHandle);
                    Addressables.Release(updateHandle);
                    onProgress?.Invoke(0f);
                    return;
                }
                
                await downloadHandle.Task;
                Addressables.Release(downloadHandle);
                onProgress?.Invoke(1f);
            }
            else
            {
                onProgress?.Invoke(1f);
            }
            
            Addressables.Release(updateHandle);
        }

        public override TObject LoadAsset<TObject>(string assetPath)
        {
            return TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath).WaitForCompletion();
        }
        
        public override async Task<TObject> LoadAssetAsync<TObject>(string assetPath)
        {
            return await TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath).Task;
        }

        public override void UnloadAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (m_AssetHandles.TryGetValue(assetPath, out var handle))
            {
                Addressables.Release(handle);
                m_AssetHandles.Remove(assetPath);
            }
        }

        public override void UnloadAsset<TObject>(TObject asset)
        {
            if (asset == null) return;
            Addressables.Release(asset);
        }

        private AsyncOperationHandle<TObject> TrackHandle<TObject>(AsyncOperationHandle<TObject> newHandle, string assetPath)
        {
            if (m_AssetHandles.TryGetValue(assetPath, out var existingHandle))
            {
                if (newHandle.IsValid() && !newHandle.IsDone)
                {
                    Addressables.Release(newHandle);
                }
                if (existingHandle is AsyncOperationHandle<TObject> typedHandle)
                {
                    return typedHandle;
                }
                Addressables.Release(existingHandle);
                m_AssetHandles.Remove(assetPath);
            }
            m_AssetHandles[assetPath] = newHandle;
            return newHandle;
        }

        public override void UnloadAllAsset()
        {
            foreach (var handle in m_AssetHandles.Values)
            {
                Addressables.Release(handle);
            }
            m_AssetHandles.Clear();
        }

        public override async Task<SceneLoaderCallbackContext> LoadSceneAsync(string sceneName, bool allowSceneActivation = true, LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(sceneName)) await Task.Yield();
            var handle = Addressables.LoadSceneAsync(sceneName, parameters.loadSceneMode, allowSceneActivation);
            m_SceneHandles[sceneName] = handle;
            while (!handle.IsDone)
            {
                float currentProgress = Mathf.Clamp01(handle.PercentComplete);
                onProgress?.Invoke(currentProgress);
                if (Mathf.Approximately(currentProgress, 1.0f) || currentProgress >= 1.0f)
                    break;
                await Task.Yield();
            }
            return new SceneLoaderCallbackContext(handle.Result);
        }

        public override async Task<SceneLoaderCallbackContext> UnloadSceneAsync(string sceneName, bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(sceneName) && !m_SceneHandles.ContainsKey(sceneName)) await Task.Yield();
            var handle = Addressables.UnloadSceneAsync(m_SceneHandles[sceneName], options);
            while (!handle.IsDone)
            {
                float currentProgress = Mathf.Clamp01(handle.PercentComplete);
                onProgress?.Invoke(currentProgress);
                if (Mathf.Approximately(currentProgress, 1.0f) || currentProgress >= 1.0f)
                    break;
                await Task.Yield();
            }
            return new SceneLoaderCallbackContext(handle.Result);
        }
    }
}
    
#endif