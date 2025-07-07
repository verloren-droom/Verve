#if UNITY_2018_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using UnityEngine;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;


    /// <summary>
    /// 可寻址资源加载子模块
    /// </summary>
    [System.Serializable]
    public sealed partial class AddressablesLoader : VerveUniEx.Loader.AssetLoaderBase
    {
        public override string ModuleName => "Addressables";
        
        private readonly Dictionary<string, AsyncOperationHandle> m_AssetHandles = new Dictionary<string, AsyncOperationHandle>();
        private readonly Dictionary<string, AsyncOperationHandle> m_SceneHandles = new Dictionary<string, AsyncOperationHandle>();
        
        
        public override TObject LoadAsset<TObject>(string assetPath)
        {
            return TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath).WaitForCompletion();
        }
        
        public override async Task<TObject> LoadAssetAsync<TObject>(string assetPath)
        {
            return await TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath).Task;
        }

        public override IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<AssetLoaderCallbackContext<TObject>> onComplete)
        {
            if (string.IsNullOrEmpty(assetPath)) yield return null;
            var handle = TrackHandle(Addressables.LoadAssetAsync<TObject>(assetPath), assetPath);
            while (!handle.IsDone)
            {
                yield return null;
            }
            onComplete?.Invoke(new AssetLoaderCallbackContext<TObject>(handle.Result));
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