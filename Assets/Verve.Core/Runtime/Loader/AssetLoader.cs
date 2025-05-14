namespace Verve.Loader
{
    
    using System;
    using System.Collections;
    using System.Threading.Tasks;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    using UnityEngine.SceneManagement;
#endif

    
    /// <summary>
    /// 资源加载基类
    /// </summary>
    public abstract class AssetLoaderBase : IAssetLoader
    {
        public abstract TObject LoadAsset<TObject>(string assetPath);
        public virtual async Task<TObject> LoadAssetAsync<TObject>(string assetPath) => await Task.Run(() => LoadAsset<TObject>(assetPath));
        public virtual IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<AssetLoaderCallbackContext<TObject>> onComplete)
        {
            if (string.IsNullOrEmpty(assetPath)) yield return null;
            var task = LoadAssetAsync<TObject>(assetPath);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            onComplete?.Invoke(task.IsFaulted
                ? new AssetLoaderCallbackContext<TObject>(false)
                : new AssetLoaderCallbackContext<TObject>(task.Result));
        }
        public virtual void UnloadAsset(string assetPath) {}
        public abstract void UnloadAsset<TObject>(TObject asset);
        public virtual void UnloadAllAsset() {}
        public virtual void Dispose() {}
        
#if UNITY_5_3_OR_NEWER
        public virtual async Task<SceneLoaderCallbackContext> LoadSceneAsync(
            string sceneName,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(sceneName)) await Task.Yield();
            var operation = SceneManager.LoadSceneAsync(sceneName, parameters);
            operation.allowSceneActivation = allowSceneActivation;
            while (!operation.isDone)
            {
                float currentProgress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(currentProgress);
                if (Mathf.Approximately(currentProgress, 0.9f) || currentProgress >= 0.9f)
                    break;
                await Task.Yield();
            }
            return new SceneLoaderCallbackContext(operation);
        }
        public virtual IEnumerator LoadSceneAsync(
            string sceneName,
            Action<SceneLoaderCallbackContext> onComplete,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default, 
            Action<float> onProgress = null)
        {
            var task = LoadSceneAsync(sceneName, allowSceneActivation, parameters, onProgress);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            onComplete?.Invoke(task.Result);
        }
        public virtual async Task<SceneLoaderCallbackContext> UnloadSceneAsync(
            string sceneName,
            bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, 
            Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(sceneName) || !SceneManager.GetSceneByName(sceneName).IsValid()) await Task.Yield();
            var operation = SceneManager.UnloadSceneAsync(sceneName, options);
            if (operation == null) await Task.Yield();
            operation.allowSceneActivation = allowSceneActivation;
            while (!operation.isDone)
            {
                float currentProgress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(currentProgress);
                if (Mathf.Approximately(currentProgress, 0.9f) || currentProgress >= 0.9f)
                    break;
                await Task.Yield();
            }
            return new SceneLoaderCallbackContext(operation);
        }
        public virtual IEnumerator UnloadSceneAsync(
            string sceneName, 
            Action<SceneLoaderCallbackContext> onComplete,
            bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, 
            Action<float> onProgress = null)
        {
            var task = UnloadSceneAsync(sceneName, allowSceneActivation, options, onProgress);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            onComplete?.Invoke(task.Result);
        }
#endif
    }
    
}