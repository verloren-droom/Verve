#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using Verve.Unit;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    
    
    [CustomUnit("Loader", -1), System.Serializable]
    public partial class LoaderUnit : Verve.Loader.LoaderUnit
    {
        protected override void OnStartup(params object[] args)
        {
            base.OnStartup(args);
            AddService(new AssetBundleLoader());
            AddService(new ResourcesLoader());
#if UNITY_2018_3_OR_NEWER
            AddService(new AddressablesLoader());
#endif
        }
        
        public IEnumerator LoadAssetAsync<TLoaderType, TAssetType>(string assetPath, Action<AssetLoaderCallbackContext<TAssetType>> onComplete) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return GetService<TLoaderType>().LoadAssetAsync<TAssetType>(assetPath, onComplete);
        }

        public async Task<SceneLoaderCallbackContext> LoadSceneAsync<TLoaderType>(
            string sceneName,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return await GetService<TLoaderType>().LoadSceneAsync(sceneName, allowSceneActivation, parameters, onProgress);
        }

        public IEnumerator LoadSceneAsync<TLoaderType>(
            string sceneName,
            Action<SceneLoaderCallbackContext> onComplete,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default, 
            Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            yield return GetService<TLoaderType>().LoadSceneAsync(sceneName, onComplete, allowSceneActivation, parameters, onProgress);
        }

        public async Task<SceneLoaderCallbackContext> UnloadSceneAsync<TLoaderType>(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return await GetService<TLoaderType>().UnloadSceneAsync(sceneName, allowSceneActivation, options, onProgress);
        }

        public IEnumerator UnloadSceneAsync<TLoaderType>(string sceneName, Action<SceneLoaderCallbackContext> onComplete, bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return GetService<TLoaderType>().UnloadSceneAsync(sceneName, onComplete, allowSceneActivation, options, onProgress);
        }
    }
}
    
#endif