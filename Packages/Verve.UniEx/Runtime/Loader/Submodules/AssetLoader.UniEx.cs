#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;


    public abstract class AssetLoaderBase : Verve.Loader.AssetLoaderBase, VerveUniEx.Loader.IAssetLoader
    {
        public virtual IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<AssetLoaderCallbackContext<TObject>> onComplete)
        {
            if (string.IsNullOrEmpty(assetPath)) yield break;
            var task = LoadAssetAsync<TObject>(assetPath);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            onComplete?.Invoke(task.IsFaulted
                ? new AssetLoaderCallbackContext<TObject>(false)
                : new AssetLoaderCallbackContext<TObject>(task.Result));
        }
        
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
    }
}
    
#endif