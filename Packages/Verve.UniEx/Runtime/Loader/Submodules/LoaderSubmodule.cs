#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx.Loader
{
    using System;
    using UnityEngine;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;


    /// <summary>
    /// 加载器子模块基类
    /// </summary>
    [Serializable]
    public abstract class LoaderSubmodule : GameFeatureSubmodule, ILoaderSubmodule
    {
        public abstract TObject LoadAsset<TObject>(string assetPath);
        public virtual async Task<TObject> LoadAssetAsync<TObject>(string assetPath) => await Task.Run(() => LoadAsset<TObject>(assetPath));
        public virtual void UnloadAsset(string assetPath) {}
        public abstract void UnloadAsset<TObject>(TObject asset);
        public virtual void UnloadAllAsset() {}
        public virtual void Dispose() {}
        
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
    }
}
    
#endif